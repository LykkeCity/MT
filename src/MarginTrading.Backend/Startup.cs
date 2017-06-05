using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Flurl.Http;
using Lykke.Logs;
using Lykke.SettingsReader;
using MarginTrading.Backend.Infrastructure;
using MarginTrading.Backend.Middleware;
using MarginTrading.Backend.Modules;
using MarginTrading.Common.Extensions;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;
using WampSharp.AspNetCore.WebSockets.Server;
using WampSharp.Binding;
using WampSharp.V2;

#pragma warning disable 1591

namespace MarginTrading.Backend
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole()
                .AddDebug();

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc();

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "MarginTrading_Api"
                });
                options.DescribeAllEnumsAsStrings();
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "MarginTrading.Backend.xml");
                options.IncludeXmlComments(xmlPath);

                options.OperationFilter<ApiKeyHeaderOperationFilter>();
            });

            var builder = new ContainerBuilder();

            MtBackendSettings mtSettings = Environment.IsDevelopment()
                ? Configuration.Get<MtBackendSettings>()
                : SettingsProcessor.Process<MtBackendSettings>(Configuration["SettingsUrl"].GetStringAsync().Result);

            bool isLive = Configuration.IsLive();
            MarginSettings settings = isLive ? mtSettings.MtBackend.MarginTradingLive : mtSettings.MtBackend.MarginTradingDemo;
            settings.EmailSender = mtSettings.EmailSender;
            settings.SlackNotifications = mtSettings.SlackNotifications;
            settings.IsLive = isLive;

            Console.WriteLine($"IsLive: {settings.IsLive}");

            RegisterModules(builder, settings, Environment);

            builder.Populate(services);
            ApplicationContainer = builder.Build();

            var meRepository = ApplicationContainer.Resolve<IMatchingEngineRepository>();
            meRepository.InitMatchingEngines(new List<object> {
                ApplicationContainer.Resolve<IMatchingEngine>(),
                new MatchingEngineBase { Id = MatchingEngines.Icm }
            });

            MtServiceLocator.FplService = ApplicationContainer.Resolve<IFplService>();
            MtServiceLocator.AccountUpdateService = ApplicationContainer.Resolve<IAccountUpdateService>();
            MtServiceLocator.AccountsCacheService = ApplicationContainer.Resolve<IAccountsCacheService>();
            MtServiceLocator.SwapCommissionService = ApplicationContainer.Resolve<ISwapCommissionService>();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            IWampHost host = app.ApplicationServices.GetService<IWampHost>();
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseMiddleware<KeyAuthMiddleware>();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUi();

            app.Map("/ws", builder =>
            {
                builder.UseWebSockets();

                host.RegisterTransport(new AspNetCoreWebSocketTransport(builder),
                                       new JTokenJsonBinding(),
                                       new JTokenMsgpackBinding());
            });

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            host.Open();

            Application application = app.ApplicationServices.GetService<Application>();

            appLifetime.ApplicationStarted.Register(() =>
                application.StartApplicatonAsync().Wait()
            );

            appLifetime.ApplicationStopping.Register(() =>
                application.StopApplication()
            );
        }

        private void RegisterModules(ContainerBuilder builder, MarginSettings settings, IHostingEnvironment environment)
        {
            LykkeLogToAzureStorage log = new LykkeLogToAzureStorage(PlatformServices.Default.Application.ApplicationName,
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "MarginTradingBackendLog", null));

            builder.RegisterModule(new BackendRepositoriesModule(settings, log));
            builder.RegisterModule(new EventModule());
            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new BaseServicesModule(settings));
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new BackendServicesModule(settings, environment, log));
        }
    }
}
