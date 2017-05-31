using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MarginTrading.Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cfgBuilder = new ConfigurationBuilder()
              .AddEnvironmentVariables();

            var configuration = cfgBuilder.Build();

            int kestrelThreadsCount = 1;
            string threadsCount = configuration["KestrelThreadCount"];

            if (threadsCount != null)
            {
                if (!int.TryParse(threadsCount, out kestrelThreadsCount))
                {
                    Console.WriteLine($"Can't parse KestrelThreadsCount value '{threadsCount}'");
                    return;
                }
            }

            Console.WriteLine($"Kestrel threads count: {kestrelThreadsCount}");

            try
            {
                var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    if (kestrelThreadsCount > 0)
                    {
                        options.ThreadCount = kestrelThreadsCount;
                    }
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls("http://*:5005")
                .UseStartup<Startup>()
                .Build();

                host.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
