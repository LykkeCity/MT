using MarginTrading.BrokerBase;

namespace MarginTrading.OrderHistoryBroker
{
    public class Program: WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5013);
        }
    }
}
