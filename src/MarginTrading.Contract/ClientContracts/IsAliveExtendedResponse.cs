// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class IsAliveExtendedResponse : IsAliveResponse
    {
        public string DemoVersion { get; set; }
        public string LiveVersion { get; set; }
        public int WampOpened { get; set; }
    }
}