using MarginTrading.BrokerBase;

namespace MarginTrading.OrderRejectedBroker
{
    public class Program : WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5014);
        }
    }
}