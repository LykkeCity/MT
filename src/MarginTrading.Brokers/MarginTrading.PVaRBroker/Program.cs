using System;
using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.PVaRBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5017")
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