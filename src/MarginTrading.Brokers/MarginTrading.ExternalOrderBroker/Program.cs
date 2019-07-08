// Copyright (c) 2019 Lykke Corp.

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
