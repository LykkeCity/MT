// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class InitAccountGroupRequest
    {
        public string TradingConditionId { get; set; }
        
        public string BaseAssetId { get; set; }
    }
}