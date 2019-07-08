// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class MarginTradingAccountModel
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public string LegalEntity { get; set; }
    }
}