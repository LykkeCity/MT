using System;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.RiskManagerBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5018")
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