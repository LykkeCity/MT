using MarginTrading.AzureRepositories;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class TradingOrderService : ITradingOrderService
	{
		public async Task CreateTradingOrderForCancelledTakerPosition(IOrder position, Func<ITradingOrder, Task> destination, bool realtime = true)
		{
			var tradingOrder = new TradingOrder();

			tradingOrder.CoreSide = position.GetOrderType() == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
			tradingOrder.CoreSymbol = position.Instrument;
			tradingOrder.ExecutionDuration = CalculateExecutionDurationForClose(position);
			tradingOrder.TakerPositionId = position.Id;
			tradingOrder.TakerAction = TakerAction.Close;
			tradingOrder.TimeForceCondition = TimeForceCondition.None;
			tradingOrder.TakerExternalSymbol = position.Instrument;
			tradingOrder.TakerCounterpartyId = position.ClientId;
			tradingOrder.IsLive = realtime;
			tradingOrder.TakerOrderId = $"{position.Id}_{TakerAction.Close}";
			tradingOrder.Volume = position.Volume;

			await destination(tradingOrder);
		}

		public async Task CreateTradingOrderForClosedTakerPosition(IOrder position, Func<ITradingOrder, Task> destination, bool realtime = true)
		{
			var tradingOrder = new TradingOrder();

			tradingOrder.CoreSide = position.GetOrderType() == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
			tradingOrder.CoreSymbol = position.Instrument;
			tradingOrder.ExecutionDuration = CalculateExecutionDurationForClose(position);
			tradingOrder.TakerPositionId = position.Id;
			tradingOrder.TakerAction = TakerAction.Close;
			tradingOrder.TimeForceCondition = GetTimeForceCongition(position.CloseReason);
			tradingOrder.TakerExternalSymbol = position.Instrument;
			tradingOrder.TakerCounterpartyId = position.ClientId;
			tradingOrder.IsLive = realtime;
			tradingOrder.TakerOrderId = $"{position.Id}_{TakerAction.Close}";
			tradingOrder.Volume = position.Volume;

			await destination(tradingOrder);
		}

		public async Task CreateTradingOrderForOpenedTakerPosition(IOrder position, Func<ITradingOrder, Task> destination, bool realtime = true)
		{
			var tradingOrder = new TradingOrder();

			tradingOrder.CoreSide = position.GetOrderType();
			tradingOrder.CoreSymbol = position.Instrument;
			tradingOrder.ExecutionDuration = CalculateExecutionDurationForOpen(position);
			tradingOrder.TakerPositionId = position.Id;
			tradingOrder.TakerAction = TakerAction.Open;
			tradingOrder.TimeForceCondition = TimeForceCondition.None;
			tradingOrder.TakerExternalSymbol = position.Instrument;
			tradingOrder.TakerCounterpartyId = position.ClientId;
			tradingOrder.IsLive = realtime;
			tradingOrder.TakerOrderId = $"{position.Id}_{TakerAction.Open}";
			tradingOrder.Volume = position.Volume;

			await destination(tradingOrder);
		}

		public async Task CreateTradingOrdersForOrderHistory(Func<Task<IEnumerable<IOrderHistory>>> source, Func<ITradingOrder, Task> destination)
		{
			foreach (IOrderHistory historyOrder in await source())
			{
				var position = MarginTradingOrderHistoryEntity.Restore(historyOrder);

				await CreateTradingOrderForOpenedTakerPosition(position, destination, false);

				await CreateTradingOrderForClosedTakerPosition(position, destination, false);
			}
		}

		private double? CalculateExecutionDurationForClose(IOrder position)
		{
			if (position.CloseDate.HasValue && position.StartClosingDate.HasValue)
				return Math.Abs((position.CloseDate.Value - position.StartClosingDate.Value).Milliseconds);
			return null;
		}

		private TimeForceCondition GetTimeForceCongition(OrderCloseReason closeReason)
		{
			switch (closeReason)
			{
				case OrderCloseReason.StopLoss:
					return TimeForceCondition.StopLoss;
				case OrderCloseReason.StopOut:
					return TimeForceCondition.StopOut;
				case OrderCloseReason.TakeProfit:
					return TimeForceCondition.TakeProfit;
				default:
					return TimeForceCondition.None;
			}
		}

		private double? CalculateExecutionDurationForOpen(IOrder position)
		{
			if (position.OpenDate.HasValue)
				return Math.Abs((position.OpenDate.Value - position.CreateDate).Milliseconds);
			return null;
		}
	}
}