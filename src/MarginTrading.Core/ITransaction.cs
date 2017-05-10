using System;

namespace MarginTrading.Core
{
    public interface ITransaction
    {
        string TakerOrderId { get; set; }
        string TakerLykkeId { get; set; }
        string TakerAccountId { get; set; }
        TakerAction TakerAction { get; set; }
        double? TakerSpread { get; set; }

        string MakerOrderId { get; set; }
        string MakerLykkeId { get; set; }
        double? MakerSpread { get; set; }

        string LykkeExecutionId { get; set; }
        OrderDirection CoreSide { get; set; }
        string CoreSymbol { get; set; }
        DateTime? ExecutedTime { get; set; }
        double? ExecutionDuration { get; set; }
        double FilledVolume { get; set; }
        double Price { get; set; }
        double? VolumeInUSD { get; set; }
        double? ExchangeMarkup { get; set; }
        double? CoreSpread { get; set; }
        string Comment { get; set; }
        bool IsLive { get; set; }
        string OrderId { get; set; }
    }

    public enum TakerAction
    {
        Open = 1,
        Close = 2,
        Cancel = 3
    }
}
