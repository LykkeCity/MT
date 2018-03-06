using MarginTrading.BrokerBase;

namespace MarginTrading.OrderbookBestPricesBroker
{
    public class Program : WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5016);
        }
    }
}