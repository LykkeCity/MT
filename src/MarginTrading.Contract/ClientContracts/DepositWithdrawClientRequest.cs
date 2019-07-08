// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class DepositWithdrawClientRequest
    {
        public string Token { get; set; }
        public string AccountId { get; set; }
        public decimal? Volume { get; set; }
    }
}
