using Microsoft.AspNetCore.Hosting;

namespace MarginTrading.IVaRBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5012")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}