using System;

namespace MarginTrading.Core
{
	public interface ITransaction
	{
		string TakerPositionId { get; set; }
		string TakerOrderId { get; set; }
		string TakerCounterpartyId { get; set; }
		string TakerAccountId { get; set; }
		TakerAction TakerAction { get; set; }
		double? TakerSpread { get; set; }

		string MakerOrderId { get; set; }
		string MakerCounterpartyId { get; set; }
		string MakerAccountId { get; set; }
		double? MakerSpread { get; set; }

		string TradingTransactionId { get; set; }
		OrderDirection CoreSide { get; set; }
		string CoreSymbol { get; set; }
		DateTime? ExecutedTime { get; set; }
		double? ExecutionDuration { get; set; }
		double FilledVolume { get; set; }
		double Price { get; set; }
		double? VolumeInUSD { get; set; }
		double? ExchangeMarkup { get; set; }
		double? CoreSpread { get; set; }
		double? TakerProfit { get; set; }
		string Comment { get; set; }
		bool IsLive { get; set; }
	}

	public enum TakerAction
	{
		Open = 1,
		Close = 2,
		Cancel = 3
	}
}