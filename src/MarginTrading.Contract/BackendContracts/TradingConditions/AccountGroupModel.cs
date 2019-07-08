// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts.TradingConditions
{
    public class AccountGroupModel
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal DepositTransferLimit { get; set; }
        public decimal ProfitWithdrawalLimit { get; set; }
    }
}