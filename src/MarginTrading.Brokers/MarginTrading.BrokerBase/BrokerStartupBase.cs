using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Flurl.Http;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Lykke.SlackNotifications;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.BrokerBase
{
    public abstract class BrokerStartupBase<TApplicationSettings, TSettings> 
        where TApplicationSettings : class, IBrokerApplicationSettings<TSettings>
        where TSettings: BrokerSettingsBase
    {
        private const string AppSettingsDevFile = "appsettings.dev.json";

        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; private set; }

        protected abstract string ApplicationName { get; }

        protected BrokerStartupBase(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(AppSettingsDevFile, true, true)
                .AddEnvironmentVariables()
                .Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Error)
                .AddDebug(LogLevel.Warning);

            services.AddSingleton(loggerFactory);
            services.AddLogging();
            services.AddSingleton(Configuration);
            services.AddMvc();

            var applicationSettings = GetApplicationSettings();

            var isLive = IsLive(Configuration);
            Console.WriteLine($"IsLive: {isLive}");

            var builder = new ContainerBuilder();
            RegisterServices(services, applicationSettings, builder, isLive);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        protected virtual TApplicationSettings GetApplicationSettings()
        {
            TApplicationSettings applicationSettings;
            string settingsLocation;
            if (Environment.IsDevelopment())
            {
                settingsLocation = AppSettingsDevFile;
                applicationSettings = Configuration.Get<TApplicationSettings>();
            }
            else
            {
                settingsLocation = Configuration["SettingsUrl"];
                applicationSettings =
                    SettingsProcessor.Process<TApplicationSettings>(settingsLocation.GetStringAsync().Result);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (applicationSettings == null)
            {
                throw new InvalidOperationException("Settings not found in " + settingsLocation);
            }
            return applicationSettings;
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IApplicationLifetime appLifetime)
        {
            app.UseMvc();

            var applications = app.ApplicationServices.GetServices<IBrokerApplication>();

            appLifetime.ApplicationStarted.Register(() =>
            {
                foreach (var application in applications)
                {
                    application.Run();
                }
            });
            
            appLifetime.ApplicationStopping.Register(() =>
            {
                foreach (var application in applications)
                {
                    application.StopApplication();
                }
            });

            appLifetime.ApplicationStopped.Register(() =>
            {
                ApplicationContainer.Dispose();
            });
        }

        protected abstract void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            TSettings settings, ILog log, bool isLive);

        protected virtual ILog CreateLogWithSlack(IServiceCollection services, TApplicationSettings settings, bool isLive)
        {
            var logToConsole = new LogToConsole();
            var logAggregate = new LogAggregate();
            var isLiveEnv = isLive ? "Live" : "Demo";

            logAggregate.AddLogger(logToConsole);

            var commonSlackService =
                services.UseSlackNotificationsSenderViaAzureQueue(settings.SlackNotifications.AzureQueue,
                    logToConsole);

            var slackService =
                new MtSlackNotificationsSender(commonSlackService, ApplicationName, isLiveEnv);

            services.AddSingleton<ISlackNotificationsSender>(slackService);

            // Creating azure storage logger, which logs own messages to concole log
            var dbLogConnectionString = settings.MtBrokersLogs?.DbConnString;
            if (!string.IsNullOrEmpty(dbLogConnectionString) &&
                !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var logToAzureStorage = services.UseLogToAzureStorage(dbLogConnectionString, slackService,
                    ApplicationName + isLiveEnv + "Log",
                    logToConsole);

                logAggregate.AddLogger(logToAzureStorage);
            }

            // Creating aggregate log, which logs to console and to azure storage, if last one specified
            return logAggregate.CreateLogger();
        }


        private void RegisterServices(IServiceCollection services, TApplicationSettings applicationSettings,
            ContainerBuilder builder,
            bool isLive)
        {
            var log = CreateLogWithSlack(services, applicationSettings, isLive);
            builder.RegisterInstance(log).As<ILog>().SingleInstance();
            builder.RegisterInstance(applicationSettings).AsSelf().SingleInstance();
            
            var settings = isLive ? applicationSettings.MtBackend.MarginTradingLive : applicationSettings.MtBackend.MarginTradingDemo;
            settings.IsLive = isLive;
            builder.RegisterInstance(settings).SingleInstance();

            builder.RegisterInstance(new CurrentApplicationInfo(isLive,
                PlatformServices.Default.Application.ApplicationVersion,
                ApplicationName
            )).AsSelf().SingleInstance();

            RegisterCustomServices(services, builder, settings, log, isLive);
            builder.Populate(services);
        }

        private static bool IsLive(IConfigurationRoot configuration)
        {
            return !string.IsNullOrEmpty(configuration["IsLive"]) &&
                   bool.TryParse(configuration["IsLive"], out var isLive) && isLive;
        }
    }
}