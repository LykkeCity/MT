using System;

namespace MarginTrading.Core
{
	public class ElementaryTransaction : IElementaryTransaction
	{
		public string CounterPartyId { get; set; }
		public string AccountId { get; set; }
		public string Asset { get; set; }
		public string CoreSymbol { get; set; }
		public AssetSide AssetSide { get; set; }
		public double? Amount { get; set; }
		public string TradingTransactionId { get; set; }
		public double? AmountInUsd { get; set; }
		public DateTime? TimeStamp { get; set; }
		public ElementaryTransactionType Type { get; set; }
		public string SubType { get; set; }
		public string TradingOrderId { get; set; }
		public string PositionId { get; set; }
	}
}