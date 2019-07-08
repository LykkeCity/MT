// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;

namespace MarginTrading.Backend.Core.TradingConditions
{
    public class TradingCondition : ITradingCondition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public string LegalEntity { get; set; }
        public decimal MarginCall1 { get; set; }
        public decimal MarginCall2 { get; set; }
        public decimal StopOut { get; set; }
        public decimal DepositLimit { get; set; }
        public decimal WithdrawalLimit { get; set; }
        public string LimitCurrency { get; set; }
        public List<string> BaseAssets { get; set; }
    }
}