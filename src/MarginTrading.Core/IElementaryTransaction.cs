using System;

namespace MarginTrading.Core
{
	public interface IElementaryTransaction
	{
		string CounterPartyId { get; set; }
		string AccountId { get; set; }
		string CoreSymbol { get; set; }
		string Asset { get; set; }
		AssetSide AssetSide { get; set; }
		double? Amount { get; set; }
		string TradingTransactionId { get; set; }
		string TradingOrderId { get; set; }
		string PositionId { get; set; }
		double? AmountInUsd { get; set; }
		DateTime? TimeStamp { get; set; }
		ElementaryTransactionType Type { get; set; }
		string SubType { get; set; }
	}

	public enum ElementaryTransactionType
	{
		Position,
		Capital
	}

	public enum AssetSide
	{
		Base,
		Quote
	}
}