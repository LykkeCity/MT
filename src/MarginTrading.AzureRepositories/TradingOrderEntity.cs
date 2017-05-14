using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.AzureRepositories
{
	public class TradingOrderEntity : TableEntity
	{
		public string TakerPositionId { get; set; }
		public string OrderType { get; set; }
		public string TakerCounterpartyId { get; set; }
		public string CoreSide { get; set; }
		public string CoreSymbol { get; set; }
		public string TakerExternalSymbol { get; set; }
		public double? TakerRequestedPrice { get; set; }
		public string TimeForceCondition { get; set; }
		public string TakerAction { get; set; }
		public double? ExecutionDuration { get; set; }
		public string GeneratedFrom { get; set; }
		public string TakerOrderId { get; set; }
		public double? Volume { get; set; }

		private static string GeneratePartitionKey(ITradingOrder order)
		{
			return $"{order.TakerCounterpartyId}_{order.CoreSymbol}";
		}

		private static string GenerateRowKey(ITradingOrder order)
		{
			return $"{order.TakerPositionId}_{order.TakerAction}";
		}

		public static ITradingOrder Restore(TradingOrderEntity entity)
		{
			if (entity == null)
				return null;

			var tradingOrder = new TradingOrder();

			tradingOrder.TakerPositionId = entity.TakerPositionId;
			tradingOrder.TakerCounterpartyId = entity.TakerCounterpartyId;
			tradingOrder.CoreSymbol = entity.CoreSymbol;
			tradingOrder.TakerExternalSymbol = entity.TakerExternalSymbol;
			tradingOrder.TakerRequestedPrice = entity.TakerRequestedPrice;
			tradingOrder.ExecutionDuration = entity.ExecutionDuration;
			tradingOrder.TakerOrderId = entity.TakerOrderId;
			tradingOrder.Volume = entity.Volume;

			OrderDirection coreSide;
			if (Enum.TryParse(entity.CoreSide, out coreSide))
			{
				tradingOrder.CoreSide = coreSide;
			}

			TimeForceCondition timeForceCondition;
			if (Enum.TryParse(entity.TimeForceCondition, out timeForceCondition))
			{
				tradingOrder.TimeForceCondition = timeForceCondition;
			}

			TakerAction takerAction;
			if (Enum.TryParse(entity.TakerAction, out takerAction))
			{
				tradingOrder.TakerAction = takerAction;
			}

			return tradingOrder;
		}

		public static TradingOrderEntity Create(ITradingOrder tradingOrder)
		{
			return new TradingOrderEntity
			{
				PartitionKey = GeneratePartitionKey(tradingOrder),
				RowKey = GenerateRowKey(tradingOrder),
				TakerPositionId = tradingOrder.TakerPositionId,
				TakerOrderId = tradingOrder.TakerOrderId,
				TakerCounterpartyId = tradingOrder.TakerCounterpartyId,
				CoreSide = tradingOrder.CoreSide.ToString(),
				CoreSymbol = tradingOrder.CoreSymbol,
				TakerExternalSymbol = tradingOrder.TakerExternalSymbol,
				TakerRequestedPrice = tradingOrder.TakerRequestedPrice,
				TimeForceCondition = tradingOrder.TimeForceCondition.ToString(),
				TakerAction = tradingOrder.TakerAction.ToString(),
				ExecutionDuration = tradingOrder.ExecutionDuration,
				GeneratedFrom = tradingOrder.IsLive ? "Live" : "History",
				Volume = tradingOrder.Volume
			};
		}
	}
}
