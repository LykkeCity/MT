using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.AccountHistoryBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5011")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
