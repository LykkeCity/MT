using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Modules;
using MarginTrading.MarketMaker.Services;
using MarginTrading.MarketMaker.Settings;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace MarginTrading.MarketMaker
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public static string ServiceName => "MarginTradingMarketMaker";
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }
        public IConfigurationRoot Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new DefaultContractResolver();
                });

            services.AddSwaggerGen(options => { options.DefaultLykkeConfiguration("v1", ServiceName + " API"); });

            var builder = new ContainerBuilder();
            var appSettings = Environment.IsDevelopment()
                ? Configuration.Get<AppSettings>()
                : HttpSettingsLoader.Load<AppSettings>(Configuration.GetValue<string>("SettingsUrl"));
            var log = CreateLogWithSlack(services, appSettings);

            builder.RegisterModule(new MarketMakerModule(appSettings, log));

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

#if DEBUG
            app.UseLykkeMiddleware(ServiceName, ex => ex.ToString());
#else
            app.UseLykkeMiddleware(ServiceName, ex => new ErrorResponse {ErrorMessage = "Technical problem"});
#endif

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi();

            appLifetime.ApplicationStarted.Register(() =>
            {
                var settings = ApplicationContainer.Resolve<MarginTradingMarketMakerSettings>();
                if (!string.IsNullOrEmpty(settings.ApplicationInsightsKey))
                {
                    TelemetryConfiguration.Active.InstrumentationKey = settings.ApplicationInsightsKey;
                }

                ApplicationContainer.Resolve<IBrokerService>().Run();
            });

            appLifetime.ApplicationStopped.Register(() => { ApplicationContainer.Dispose(); });
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, AppSettings settings)
        {
            LykkeLogToAzureStorage logToAzureStorage = null;

            var logToConsole = new LogToConsole();
            var logAggregate = new LogAggregate();

            logAggregate.AddLogger(logToConsole);

            var dbLogConnectionString = settings.MarginTradingMarketMaker.Db.LogsConnString;

            var slackService = settings.SlackNotifications != null
                ? services.UseSlackNotificationsSenderViaAzureQueue(settings.SlackNotifications.AzureQueue, logToConsole)
                : null;

            slackService =
                new MtSlackNotificationsSender(slackService, ServiceName);

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) &&
                !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                logToAzureStorage =
                    services.UseLogToAzureStorage(dbLogConnectionString, slackService, ServiceName + "Log",
                        logToConsole);

                logAggregate.AddLogger(logToAzureStorage);
            }

            // Creating aggregate log, which logs to console and to azure storage, if last one specified
            var log = logAggregate.CreateLogger();



            return log;
        }
    }
}