using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Flurl.Http;
using Lykke.Logs;
using Lykke.SettingsReader;
using MarginTrading.Public.Modules;
using MarginTrading.Public.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;

namespace MarginTrading.Public
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "Api"
                });
                options.DescribeAllEnumsAsStrings();
            });

            var builder = new ContainerBuilder();

            ApplicationSettings appSettings = Environment.IsDevelopment()
                ? Configuration.Get<ApplicationSettings>()
                : SettingsProcessor.Process<ApplicationSettings>(Configuration["SettingsUrl"].GetStringAsync().Result);

            MtPublicBaseSettings settings = appSettings.MtPublic;

            if (!string.IsNullOrEmpty(Configuration["Env"]))
            {
                settings.Env = Configuration["Env"];
                Console.WriteLine($"Env: {settings.Env}");
            }

            var consoleLogger = new LogToConsole();

            services.UseLogToAzureStorage(settings.Db.LogsConnString,
                null, "MarginTradingPublicLog", consoleLogger);

            builder.RegisterModule(new PublicApiModule(settings));

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi();
        }
    }
}
