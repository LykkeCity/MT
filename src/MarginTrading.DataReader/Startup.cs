using System;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Modules;
using MarginTrading.Common.Services;
using MarginTrading.DataReader.Infrastructure;
using MarginTrading.DataReader.Middleware;
using MarginTrading.DataReader.Modules;
using MarginTrading.DataReader.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.Swagger.Model;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

#pragma warning disable 1591

namespace MarginTrading.DataReader
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddDevJson(env)
                .AddEnvironmentVariables()
                .Build();

            Environment = env;
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Error)
                .AddDebug(LogLevel.Error);

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
            services.AddAuthentication(KeyAuthOptions.AuthenticationScheme)
                .AddScheme<KeyAuthOptions, KeyAuthHandler>(KeyAuthOptions.AuthenticationScheme, "", options => { });

            var isLive = Configuration.IsLive();

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", $"MarginTrading_DataReader_Api_{(isLive ? "Live" : "Demo")}");
                options.OperationFilter<ApiKeyHeaderOperationFilter>();
                options.OperationFilter<CustomOperationIdOperationFilter>();
                options.SchemaFilter<FixResponseValueTypesNullabilitySchemaFilter>();
            });

            var builder = new ContainerBuilder();

            var readerSettings = Configuration.LoadSettings<AppSettings>();

            var settings = readerSettings.Nested(s => s.MtDataReader);

            SetupLoggers(services, readerSettings, settings);

            RegisterModules(builder, readerSettings, settings);

            builder.Populate(services);
            ApplicationContainer = builder.Build();
            return new AutofacServiceProvider(ApplicationContainer);
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IApplicationLifetime appLifetime)
        {
            app.UseLykkeMiddleware("MarginTradingDataReader",
#if DEBUG
                ex => ex.ToString());
#else
                ex => new { ErrorMessage = "Technical problem" });
#endif
            app.UseAuthentication();
            app.UseMvc();
            app.UseSwagger();
            app.UseSwagger(DocumentFilter, "swagger/{apiVersion}/swagger-no-api-key.json");
            app.UseSwaggerUi();

            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            appLifetime.ApplicationStarted.Register(() =>
            {
                LogLocator.CommonLog?.WriteMonitorAsync("", "", "Started");
            });

            appLifetime.ApplicationStopping.Register(() =>
            {
                LogLocator.CommonLog?.WriteMonitorAsync("", "", "Terminating");
            });
        }

        /// <summary>
        /// If generating swagger without api-key - strip it.
        /// </summary>
        /// <remarks>
        /// This is a nasty workaround for autorest generator not to create apiKey parameters for every method.
        /// </remarks>
        private void DocumentFilter(HttpRequest httpRequest, SwaggerDocument swaggerDocument)
        {
            foreach (var path in swaggerDocument.Paths.Values)
            {
                path.Get.Parameters?.Remove(path.Get.Parameters.First(p => p.Name == KeyAuthOptions.DefaultHeaderName));
            }
        }

        private void RegisterModules(ContainerBuilder builder, IReloadingManager<AppSettings> readerSettings,
            IReloadingManager<DataReaderSettings> settings)
        {
            builder.RegisterModule(new DataReaderSettingsModule(settings));
            builder.RegisterModule(new DataReaderRepositoriesModule(settings, LogLocator.CommonLog));
            builder.RegisterModule(new DataReaderServicesModule());
            builder.RegisterModule(new MarginTradingCommonModule());
            builder.RegisterModule(new DataReaderExternalServicesModule(readerSettings));
        }

        private static void SetupLoggers(IServiceCollection services, IReloadingManager<AppSettings> mtSettings,
            IReloadingManager<DataReaderSettings> settings)
        {
            var consoleLogger = new LogToConsole();

            MtSlackNotificationsSender slackService = null;
            if (mtSettings.CurrentValue.SlackNotifications != null)
            {
                var commonSlackService =
                    services.UseSlackNotificationsSenderViaAzureQueue(
                        mtSettings.CurrentValue.SlackNotifications.AzureQueue,
                        consoleLogger);

                slackService = new MtSlackNotificationsSender(commonSlackService, "MT DataReader", Program.EnvInfo);
            }

            var log = services.UseLogToAzureStorage(settings.Nested(s => s.Db.LogsConnString), slackService,
                "MarginTradingDataReaderLog", consoleLogger);

            LogLocator.CommonLog = log;
        }
    }
}