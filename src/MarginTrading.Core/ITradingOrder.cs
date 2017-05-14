namespace MarginTrading.Core
{
	public interface ITradingOrder
	{
		string TakerOrderId { get; set; }
		string TakerPositionId { get; set; }
		string TakerCounterpartyId { get; set; }
		OrderDirection CoreSide { get; set; }
		string CoreSymbol { get; set; }
		string TakerExternalSymbol { get; set; }
		double? TakerRequestedPrice { get; set; }
		TimeForceCondition TimeForceCondition { get; set; }
		TakerAction TakerAction { get; set; }
		double? ExecutionDuration { get; set; }
		bool IsLive { get; set; }
		double? Volume { get; set; }
	}

	public enum TimeForceCondition
	{
		None = 0,
		TakeProfit = 1,
		StopLoss = 2,
		StopOut = 3
	}
}