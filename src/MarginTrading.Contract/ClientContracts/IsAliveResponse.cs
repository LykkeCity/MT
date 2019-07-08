// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Contract.ClientContracts
{
    public class IsAliveResponse
    {
        public string Version { get; set; }
        public string Env { get; set; }
        public DateTime ServerTime { get; set; }
    }
}
