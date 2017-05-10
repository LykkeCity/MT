using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.TransactionBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5004")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}