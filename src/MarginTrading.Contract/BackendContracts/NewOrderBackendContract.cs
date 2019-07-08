// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts
{
    public class NewOrderBackendContract
    {
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public OrderFillTypeContract FillType { get; set; }
    }
}