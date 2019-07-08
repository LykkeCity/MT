// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class ChangeOrderLimitsClientRequest
    {
        public string OrderId { get; set; }
        public string AccountId { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
    }
}
