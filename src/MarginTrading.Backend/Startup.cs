// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log; 
using FluentScheduler;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Logs.MsSql;
using Lykke.Logs.MsSql.Repositories;
using Lykke.Logs.Serilog;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Correlation.Cqrs;
using Lykke.Snow.Common.Correlation.Http;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Common.Correlation.Serilog;
using Lykke.Snow.Common.Startup.ApiKey;
using Lykke.Snow.Common.Startup.Hosting;
using Lykke.Snow.Common.Startup.Log;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.Backend.Binders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Extensions;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Middleware;
using MarginTrading.Backend.Modules;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Caches;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Modules;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.Scheduling;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog.Core;
using StackExchange.Redis;
using GlobalErrorHandlerMiddleware = MarginTrading.Backend.Middleware.GlobalErrorHandlerMiddleware;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using KeyAuthHandler = MarginTrading.Backend.Middleware.KeyAuthHandler;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public class Startup
    {
        private IReloadingManager<MtBackendSettings> _mtSettingsManager;
        
        public IConfigurationRoot Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        public ILifetimeScope ApplicationContainer { get; set; }

        public Startup(IWebHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddSerilogJson(env)
                .AddEnvironmentVariables()
                .Build();
            
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var correlationContextAccessor = new CorrelationContextAccessor();

            services.AddSingleton(correlationContextAccessor);
            services.AddSingleton<RabbitMqCorrelationManager>();
            services.AddSingleton<CqrsCorrelationManager>();
            services.AddTransient<HttpCorrelationHandler>();
            
            services.AddApplicationInsightsTelemetry();

            services.AddSingleton(Configuration);
            services
                .AddControllers(opt =>
                {
                    opt.ModelBinderProviders.Insert(0, new CoreModelBinderProvider());
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
            services.AddScoped<MarginTradingEnabledFilter>();
            services.AddAuthentication(KeyAuthOptions.AuthenticationScheme)
                .AddScheme<KeyAuthOptions, KeyAuthHandler>(KeyAuthOptions.AuthenticationScheme, "", options => { });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1",
                    new OpenApiInfo
                        { Title = $"MarginTradingEngine_Api_{Configuration.ServerType()}", Version = "v1" });
                options.AddApiKeyAwareness();
            }).AddSwaggerGenNewtonsoftSupport();

            _mtSettingsManager = Configuration.LoadSettings<MtBackendSettings>(
                    throwExceptionOnCheckError: !Configuration.NotThrowExceptionsOnServiceValidation())
                .Nested(s =>
                {
                    s.MtBackend.Env = Configuration.ServerType();
                    return s;
                });
            
            services.AddFeatureManagement(_mtSettingsManager.CurrentValue.MtBackend);

            SetupLoggers(Configuration, services, _mtSettingsManager, correlationContextAccessor);
        }

        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var deduplicationService = RunHealthChecks(_mtSettingsManager.CurrentValue.MtBackend);

            builder.RegisterInstance(deduplicationService).AsSelf().As<IDisposable>().SingleInstance();
            
            RegisterModules(builder, _mtSettingsManager, Environment);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            ApplicationContainer = app.ApplicationServices.GetAutofacRoot();

            ApplicationContainer.Resolve<IScheduleSettingsCacheService>()
                .UpdateAllSettingsAsync().GetAwaiter().GetResult();

            InitializeJobs();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCorrelation();
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseMiddleware<MaintenanceModeMiddleware>();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) =>
                    swagger.Servers = new List<OpenApiServer> {
                        new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    });
            });
            app.UseSwaggerUI(a => a.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading Engine API Swagger"));
            
            var application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStarted.Register(async () =>
            {
                try
                {
                    await ApplicationContainer
                        .Resolve<IConfigurationValidator>()
                        .WarnIfInvalidAsync();
                    
                    ApplicationContainer
                        .Resolve<IClientProfileSettingsCache>()
                        .Start();
                
                    ApplicationContainer
                        .Resolve<ICqrsEngine>()
                        .StartAll();

                    Program.AppHost.WriteLogs(Environment, LogLocator.CommonLog);
                    LogLocator.CommonLog?.WriteMonitorAsync("", "", $"{Configuration.ServerType()} Started");
                }
                catch (Exception e)
                {
                    LogLocator.CommonLog?.WriteFatalErrorAsync("", "", e);
                    appLifetime.StopApplication();
                }
            });

            appLifetime.ApplicationStopping.Register(() =>
                {
                    LogLocator.CommonLog?.WriteMonitorAsync("", "", $"{Configuration.ServerType()} Terminating");
                    application.StopApplication();
                }
            );
            
            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());
        }

        private static void RegisterModules(
            ContainerBuilder builder,
            IReloadingManager<MtBackendSettings> mtSettings,
            IWebHostEnvironment environment)
        {
            var settings = mtSettings.Nested(x => x.MtBackend);

            builder.RegisterModule(new BaseServicesModule(mtSettings.CurrentValue, LogLocator.CommonLog));
            builder.RegisterModule(new BackendSettingsModule(mtSettings));
            builder.RegisterModule(new BackendRepositoriesModule(settings, LogLocator.CommonLog));
            builder.RegisterModule(new EventModule());
            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new ServicesModule(settings.CurrentValue));
            builder.RegisterModule(new BackendServicesModule(
                mtSettings.CurrentValue,
                settings.CurrentValue,
                environment,
                LogLocator.CommonLog));
            builder.RegisterModule(new MarginTradingCommonModule());
            builder.RegisterModule(new ExternalServicesModule(mtSettings));
            builder.RegisterModule(new BackendMigrationsModule());
            builder.RegisterModule(new CqrsModule(settings.CurrentValue.Cqrs, settings.CurrentValue, LogLocator.CommonLog));

            builder.RegisterBuildCallback(c =>
            {
                void StartService<T>() where T: IStartable
                {
                    LogLocator.CommonLog.WriteInfo("RegisterModules", "Start services",
                        $"Starting {typeof(T)}");
                    c.Resolve<T>().Start();
                    LogLocator.CommonLog.WriteInfo("RegisterModules", "Start services",
                        $"{typeof(T)} is started");
                }

                ContainerProvider.Container = c;

                // note the order here is important!
                StartService<TradingInstrumentsManager>();
                StartService<OrderBookSaveService>();
                StartService<IExternalOrderbookService>();
                StartService<QuoteCacheService>();
                StartService<FxRateCacheService>();
                
                StartService<AccountManager>();
                StartService<OrderCacheManager>();
                StartService<PendingOrdersCleaningService>();
            });
        }

        private static void SetupLoggers(IConfiguration configuration, IServiceCollection services,
            IReloadingManager<MtBackendSettings> mtSettings, CorrelationContextAccessor correlationContextAccessor)
        {
            var settings = mtSettings.Nested(x => x.MtBackend);
            const string requestsLogName = "MarginTradingBackendRequestsLog";
            const string logName = "MarginTradingBackendLog";
            var consoleLogger = new LogToConsole();

            #region Logs settings validation

            if (!settings.CurrentValue.UseSerilog && string.IsNullOrWhiteSpace(settings.CurrentValue.Db.LogsConnString))
            {
                throw new Exception("Either UseSerilog must be true or LogsConnString must be set");
            }

            #endregion Logs settings validation

            if (settings.CurrentValue.UseSerilog)
            {
                LogLocator.RequestsLog = LogLocator.CommonLog = new SerilogLogger(typeof(Startup).Assembly, configuration, 
                    new List<Func<(string Name, object Value)>>
                    {
                        () => ("BrokerId", settings.CurrentValue.BrokerId)
                    },
                    new List<ILogEventEnricher>
                    {
                        new CorrelationLogEventEnricher("CorrelationId", correlationContextAccessor)
                    });
            }
            else if (settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer)
            {
                LogLocator.RequestsLog = new AggregateLogger(
                    new LogToSql(new SqlLogRepository(requestsLogName,
                        settings.CurrentValue.Db.LogsConnString)),
                    new LogToConsole());

                LogLocator.CommonLog = new AggregateLogger(
                    new LogToSql(new SqlLogRepository(logName,
                        settings.CurrentValue.Db.LogsConnString)),
                    new LogToConsole());
            }

            services.AddSingleton<ILoggerFactory>(x => new WebHostLoggerFactory(LogLocator.CommonLog));
        }

        /// <summary>
        /// Initialize scheduled jobs. Each job will start in time with dispersion of 100ms.
        /// </summary>
        private void InitializeJobs()
        {
            JobManager.UseUtcTime();
            JobManager.Initialize();

            JobManager.AddJob(() => ApplicationContainer.Resolve<ScheduleSettingsCacheWarmUpJob>().Execute(),
                s => s.NonReentrant().ToRunEvery(1).Days().At(0, 0));
            
            JobManager.AddJob(ApplicationContainer.Resolve<AccountsCacheService>().ResetTodayProps, s => s
                .WithName(nameof(AccountManager)).NonReentrant().ToRunEvery(1).Days().At(0, 0));

            ApplicationContainer.Resolve<IOvernightMarginService>().ScheduleNext();
            ApplicationContainer.Resolve<IScheduleControlService>().ScheduleNext();
        }

        private StartupDeduplicationService RunHealthChecks(MarginTradingSettings marginTradingSettings)
        {
            var deduplicationService = new StartupDeduplicationService(Environment, 
                LogLocator.CommonLog, 
                marginTradingSettings, 
                ConnectionMultiplexer.Connect(marginTradingSettings.RedisSettings.Configuration));
            
            deduplicationService.HoldLock();

            new QueueValidationService(marginTradingSettings.StartupQueuesChecker.ConnectionString,
                    new[]
                    {
                        marginTradingSettings.StartupQueuesChecker.OrderHistoryQueueName,
                        marginTradingSettings.StartupQueuesChecker.PositionHistoryQueueName
                    })
                .ThrowExceptionIfQueuesNotEmpty(!marginTradingSettings.StartupQueuesChecker.DisablePoisonQueueCheck);

            return deduplicationService;
        }
    }
}