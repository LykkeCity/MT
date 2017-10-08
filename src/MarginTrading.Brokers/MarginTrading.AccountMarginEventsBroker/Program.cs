using MarginTrading.BrokerBase;

namespace MarginTrading.AccountMarginEventsBroker
{
    public class Program : WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5015);
        }
    }
}