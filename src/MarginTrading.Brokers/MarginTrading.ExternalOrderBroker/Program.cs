using MarginTrading.BrokerBase;

namespace MarginTrading.ExternalOrderBroker
{
    public class Program: WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5016);
        }
    }
}
