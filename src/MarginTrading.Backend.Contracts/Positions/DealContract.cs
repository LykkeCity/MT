using System;
using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.Backend.Contracts.Positions
{
    public class DealContract
    {
        public string DealId { get; set; }
        public string PositionId { get; set; }
        public DateTime Created { get; set; }
        public string OpenTradeId { get; set; }
        public OrderTypeContract OpenOrderType { get; set; }
        public decimal OpenOrderVolume { get; set; }
        public decimal? OpenOrderExpectedPrice { get; set; }
        public string CloseTradeId { get; set; }
        public OrderTypeContract CloseOrderType { get; set; }
        public decimal CloseOrderVolume { get; set; }
        public decimal? CloseOrderExpectedPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal OpenFxPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal CloseFxPrice { get; set; }
        public decimal Fpl { get; set; }
        public decimal PnlOfTheLastDay { get; set; }
        public string AdditionalInfo { get; set; }
        public OriginatorTypeContract Originator { get; set; }
    }
}