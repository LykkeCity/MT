using MarginTrading.BrokerBase;

namespace MarginTrading.AccountHistoryBroker
{
    public class Program : WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5011);
        }
    }
}