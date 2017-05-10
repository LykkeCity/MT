using System;

namespace MarginTrading.Core
{
    public class Transaction : ITransaction
    {
        public string TakerOrderId { get; set; }
        public string TakerLykkeId { get; set; }
        public string TakerAccountId { get; set; }
        public TakerAction TakerAction { get; set; }
        public double? TakerSpread { get; set; }

        public string MakerOrderId { get; set; }
        public string MakerLykkeId { get; set; }
        public double? MakerSpread { get; set; }

        public string LykkeExecutionId { get; set; }
        public OrderDirection CoreSide { get; set; }
        public string CoreSymbol { get; set; }
        public DateTime? ExecutedTime { get; set; }
        public double? ExecutionDuration { get; set; }
        public double FilledVolume { get; set; }
        public double Price { get; set; }
        public double? VolumeInUSD { get; set; }
        public double? ExchangeMarkup { get; set; }
        public double? CoreSpread { get; set; }
        public string Comment { get; set; }
        public bool IsLive { get; set; }
        public string OrderId { get; set; }
    }
}
