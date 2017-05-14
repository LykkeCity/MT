namespace MarginTrading.Core
{
	public class TradingOrder : ITradingOrder
	{
		public string TakerOrderId { get; set; }
		public string TakerPositionId { get; set; }
		public string TakerCounterpartyId { get; set; }
		public OrderDirection CoreSide { get; set; }
		public string CoreSymbol { get; set; }
		public string TakerExternalSymbol { get; set; }
		public double? TakerRequestedPrice { get; set; }
		public TimeForceCondition TimeForceCondition { get; set; }
		public TakerAction TakerAction { get; set; }
		public double? ExecutionDuration { get; set; }
		public bool IsLive { get; set; }
		public double? Volume { get; set; }
	}
}