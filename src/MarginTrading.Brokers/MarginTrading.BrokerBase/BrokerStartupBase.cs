using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Flurl.Http;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using MarginTrading.BrokerBase.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.BrokerBase
{
    public abstract class BrokerStartupBase<TSettingsRoot> where TSettingsRoot : class, IBrokerSettingsRoot
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

            var settingsRoot = GetSettingsRoot();

            var isLive = IsLive(Configuration);
            Console.WriteLine($"IsLive: {isLive}");

            var builder = new ContainerBuilder();
            RegisterServices(services, settingsRoot, builder, isLive);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        protected virtual TSettingsRoot GetSettingsRoot()
        {
            TSettingsRoot settingsRoot;
            string settingsLocation;
            if (Environment.IsDevelopment())
            {
                settingsLocation = AppSettingsDevFile;
                settingsRoot = Configuration.Get<TSettingsRoot>();
            }
            else
            {
                settingsLocation = Configuration["SettingsUrl"];
                settingsRoot = SettingsProcessor.Process<TSettingsRoot>(settingsLocation.GetStringAsync().Result);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (settingsRoot == null)
            {
                throw new InvalidOperationException("Settings not found in " + settingsLocation);
            }
            return settingsRoot;
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IApplicationLifetime appLifetime)
        {
            app.UseMvc();

            var application = app.ApplicationServices.GetService<IBrokerApplication>();

            appLifetime.ApplicationStarted.Register(() => application.Run());
            appLifetime.ApplicationStopping.Register(() => application.StopApplication());
            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());
        }

        protected abstract void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder,
            TSettingsRoot settingsRoot, ILog log, bool isLive);

        protected virtual ILog CreateLogWithSlack(IServiceCollection services, TSettingsRoot settings, bool isLive)
        {
            var logToConsole = new LogToConsole();
            var logAggregate = new LogAggregate();

            logAggregate.AddLogger(logToConsole);

            var slackService = settings.SlackNotifications != null
                ? services.UseSlackNotificationsSenderViaAzureQueue(settings.SlackNotifications.AzureQueue,
                    logToConsole)
                : null;

            // Creating azure storage logger, which logs own messages to concole log
            var dbLogConnectionString = settings.MtBrokersLogs?.DbConnString;
            if (!string.IsNullOrEmpty(dbLogConnectionString) &&
                !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var logToAzureStorage = services.UseLogToAzureStorage(dbLogConnectionString, slackService,
                    ApplicationName + (isLive ? "Live" : "Demo") + "Log",
                    logToConsole);

                logAggregate.AddLogger(logToAzureStorage);
            }

            // Creating aggregate log, which logs to console and to azure storage, if last one specified
            return logAggregate.CreateLogger();
        }


        private void RegisterServices(IServiceCollection services, TSettingsRoot settingsRoot,
            ContainerBuilder builder,
            bool isLive)
        {
            var log = CreateLogWithSlack(services, settingsRoot, isLive);
            builder.RegisterInstance(log).As<ILog>().SingleInstance();
            builder.RegisterInstance(settingsRoot).AsSelf().SingleInstance();

            builder.RegisterInstance(new CurrentApplicationInfo(isLive,
                PlatformServices.Default.Application.ApplicationVersion,
                ApplicationName
            )).AsSelf().SingleInstance();

            RegisterCustomServices(services, builder, settingsRoot, log, isLive);
            builder.Populate(services);
        }

        private static bool IsLive(IConfigurationRoot configuration)
        {
            return !string.IsNullOrEmpty(configuration["IsLive"]) &&
                   bool.TryParse(configuration["IsLive"], out var isLive) && isLive;
        }
    }
}