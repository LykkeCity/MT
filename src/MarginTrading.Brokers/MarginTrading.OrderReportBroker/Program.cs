using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.OrderReportBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5015")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}