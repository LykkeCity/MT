using System;

namespace MarginTrading.Core
{
	public class Transaction : ITransaction
	{
		public string TakerPositionId { get; set; }
		public string TakerOrderId { get; set; }
		public string TakerCounterpartyId { get; set; }
		public string TakerAccountId { get; set; }
		public TakerAction TakerAction { get; set; }
		public double? TakerSpread { get; set; }

		public string MakerOrderId { get; set; }
		public string MakerCounterpartyId { get; set; }
		public string MakerAccountId { get; set; }
		public double? MakerSpread { get; set; }

		public string TradingTransactionId { get; set; }
		public OrderDirection CoreSide { get; set; }
		public string CoreSymbol { get; set; }
		public DateTime? ExecutedTime { get; set; }
		public double? ExecutionDuration { get; set; }
		public double FilledVolume { get; set; }
		public double Price { get; set; }
		public double? VolumeInUSD { get; set; }
		public double? ExchangeMarkup { get; set; }
		public double? CoreSpread { get; set; }
		public double? TakerProfit { get; set; }
		public string Comment { get; set; }
		public bool IsLive { get; set; }
	}
}
