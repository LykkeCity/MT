// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class InitAccountsLiveDemoClientResponse
    {
        public MarginTradingAccountClientContract[] Live { get; set; }
        public MarginTradingAccountClientContract[] Demo { get; set; }
    }
}