using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.OrderHistoryBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5002")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
