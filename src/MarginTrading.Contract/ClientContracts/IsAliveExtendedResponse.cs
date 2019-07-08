// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class IsAliveExtendedResponse : IsAliveResponse
    {
        public string DemoVersion { get; set; }
        public string LiveVersion { get; set; }
        public int WampOpened { get; set; }
    }
}