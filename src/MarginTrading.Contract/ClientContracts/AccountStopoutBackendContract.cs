// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class AccountStopoutBackendContract
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public int PositionsCount { get; set; }
        public decimal TotalPnl { get; set; }
    }
}