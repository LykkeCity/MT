using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.OrderHistoryBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5013")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
