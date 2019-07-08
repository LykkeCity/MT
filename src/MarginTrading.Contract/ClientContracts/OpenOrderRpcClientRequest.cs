// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class OpenOrderRpcClientRequest
    {
        public string Token { get; set; }
        public NewOrderClientContract Order { get; set; }
    }
}
