// Copyright (c) 2019 Lykke Corp.

using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Contract.ClientContracts
{
    public class NewOrderClientContract
    {
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal? Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public OrderFillTypeContract FillType { get; set; }
    }
}