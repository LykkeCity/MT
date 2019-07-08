// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class InitAccountGroupRequest
    {
        public string TradingConditionId { get; set; }
        
        public string BaseAssetId { get; set; }
    }
}