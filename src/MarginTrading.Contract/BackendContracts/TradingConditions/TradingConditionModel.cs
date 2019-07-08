// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts.TradingConditions
{
    public class TradingConditionModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public string LegalEntity { get; set; }
    }
}