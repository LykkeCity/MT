using MarginTrading.BrokerBase;

namespace MarginTrading.AccountReportsBroker
{
    public class Program: WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5012);
        }
    }
}