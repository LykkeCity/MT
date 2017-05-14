using MarginTrading.AzureRepositories;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class TransactionService : ITransactionService
	{
		private readonly IQuoteCacheService _quoteCacheService;
		private readonly IInstrumentsCache _instrumentsCache;
		private readonly IAccountsCacheService _accountsCacheService;

		public TransactionService(
			IInstrumentsCache instrumentsCache,
			IQuoteCacheService quoteCacheService,
			IAccountsCacheService accountsCacheService)
		{
			_instrumentsCache = instrumentsCache;
			_quoteCacheService = quoteCacheService;
			_accountsCacheService = accountsCacheService;
		}

		public async Task CreateTransactionsForCancelledOrderAsync(IOrder order, Func<ITransaction, Task> destination, bool realtime = true)
		{
			foreach (MatchedOrder matchedOrder in order.MatchedOrders)
			{
				var transaction = new Transaction();

				double? spread = null;

				if (realtime)
				{
					CalculateSpread(order.Instrument);
				}

				transaction.TakerOrderId = $"{order.Id}_Close";
				transaction.TakerPositionId = order.Id;
				transaction.TakerCounterpartyId = !string.IsNullOrWhiteSpace(order.ClientId) ? order.ClientId : "AnnonymousTrader";
				transaction.TakerAccountId = !string.IsNullOrWhiteSpace(order.AccountId) ? order.AccountId : "AnnonymousTrader_Account";
				transaction.TakerAction = TakerAction.Cancel;
				transaction.TakerSpread = realtime ? spread / 2 : new double?();

				transaction.MakerOrderId = matchedOrder.OrderId;
				transaction.MakerCounterpartyId = !string.IsNullOrWhiteSpace(matchedOrder.ClientId) ? matchedOrder.ClientId : "LykkeMarketMaker1";
				transaction.MakerAccountId = GetMakerAccountId(transaction.MakerCounterpartyId);
				transaction.MakerSpread = realtime ? spread / 2 : new double?();

				transaction.TradingTransactionId = $"{order.Id}_{matchedOrder.OrderId}_Cancel";
				transaction.CoreSide = order.GetOrderType() == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
				transaction.CoreSymbol = order.Instrument;
				transaction.ExecutedTime = order.CloseDate;
				transaction.ExecutionDuration = CalculateExecutionDurationForClose(order);
				transaction.FilledVolume = matchedOrder.Volume;
				transaction.Price = matchedOrder.Price;
				transaction.VolumeInUSD = realtime ? CalculateVolumeInUsd(matchedOrder.Volume, order.Instrument, transaction.CoreSide) : new double?();
				transaction.ExchangeMarkup = 0;
				transaction.CoreSpread = realtime ? spread : new double?();
				transaction.Comment = string.Empty;
				transaction.IsLive = realtime;
				transaction.TakerProfit = order.GetFpl();

				await destination(transaction);
			}
		}

		private string GetMakerAccountId(string makerCounterpartyId)
		{
			var account = _accountsCacheService.GetAll(makerCounterpartyId).FirstOrDefault();

			if (account != null)
			{
				return account.Id;
			}

			return $"{makerCounterpartyId}_Account";
		}

		public async Task CreateTransactionsForClosedOrderAsync(IOrder order, Func<ITransaction, Task> destination, bool realtime = true)
		{
			if (string.IsNullOrWhiteSpace(order.Id))
				return;

			int index = 0;
			foreach (MatchedOrder matchedOrder in order.MatchedCloseOrders)
			{
				if (matchedOrder.OrderId == null)
					continue;

				var transaction = new Transaction();

				double? spread = null;

				if (realtime)
				{
					CalculateSpread(order.Instrument);
				}

				transaction.TakerOrderId = $"{order.Id}_Close";
				transaction.TakerPositionId = order.Id;
				transaction.TakerCounterpartyId = !string.IsNullOrWhiteSpace(order.ClientId) ? order.ClientId : "AnnonymousTrader";
				transaction.TakerAccountId = !string.IsNullOrWhiteSpace(order.AccountId) ? order.AccountId : "AnnonymousTrader_Account";
				transaction.TakerAction = TakerAction.Close;
				transaction.TakerSpread = realtime ? spread / 2 : new double?();

				transaction.MakerOrderId = matchedOrder.OrderId;
				transaction.MakerCounterpartyId = !string.IsNullOrWhiteSpace(matchedOrder.ClientId) ? matchedOrder.ClientId : "LykkeMarketMaker1";
				transaction.MakerAccountId = GetMakerAccountId(transaction.MakerCounterpartyId);
				transaction.MakerSpread = realtime ? spread / 2 : new double?();

				transaction.TradingTransactionId = $"{order.Id}_{matchedOrder.OrderId}_{index}_Close";
				transaction.CoreSide = order.GetOrderType() == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
				transaction.CoreSymbol = order.Instrument;
				transaction.ExecutedTime = order.CloseDate;
				transaction.ExecutionDuration = CalculateExecutionDurationForClose(order);
				transaction.FilledVolume = matchedOrder.Volume;
				transaction.Price = matchedOrder.Price;
				transaction.VolumeInUSD = realtime ? CalculateVolumeInUsd(matchedOrder.Volume, order.Instrument, transaction.CoreSide) : new double?();
				transaction.ExchangeMarkup = order.CloseCommission;
				transaction.CoreSpread = realtime ? spread : new double?();
				transaction.Comment = string.Empty;
				transaction.IsLive = realtime;
				transaction.TakerProfit = (order.GetOrderType() == OrderDirection.Buy ? 1 : -1) * matchedOrder.Volume * (matchedOrder.Price - order.OpenPrice);

				await destination(transaction);

				index++;
			}
		}

		public async Task CreateTransactionsForOpenOrderAsync(IOrder order, Func<ITransaction, Task> destination, bool realtime = true)
		{
			if (string.IsNullOrWhiteSpace(order.Id))
				return;

			int index = 0;
			foreach (MatchedOrder matchedOrder in order.MatchedOrders)
			{
				if (matchedOrder.OrderId == null)
					continue;

				var transaction = new Transaction();

				double? spread = null;

				if (realtime)
				{
					spread = CalculateSpread(order.Instrument);
				}

				transaction.TakerOrderId = $"{order.Id}_Open";
				transaction.TakerPositionId = order.Id;
				transaction.TakerCounterpartyId = !string.IsNullOrWhiteSpace(order.ClientId) ? order.ClientId : "AnnonymousTrader";
				transaction.TakerAccountId = !string.IsNullOrWhiteSpace(order.AccountId) ? order.AccountId : "AnnonymousTrader_Account";
				transaction.TakerAction = TakerAction.Open;
				transaction.TakerSpread = realtime ? spread / 2 : new double?();

				transaction.MakerOrderId = matchedOrder.OrderId;
				transaction.MakerCounterpartyId = !string.IsNullOrWhiteSpace(matchedOrder.ClientId) ? order.ClientId : "LykkeMarketMaker1";
				transaction.MakerAccountId = GetMakerAccountId(transaction.MakerCounterpartyId);
				transaction.MakerSpread = realtime ? spread / 2 : new double?();

				transaction.TradingTransactionId = $"{order.Id}_{matchedOrder.OrderId}_{index}_Open";
				transaction.CoreSide = order.GetOrderType();
				transaction.CoreSymbol = order.Instrument;
				transaction.ExecutedTime = order.OpenDate;
				transaction.ExecutionDuration = CalculateExecutionDurationForOpen(order);
				transaction.FilledVolume = matchedOrder.Volume;
				transaction.Price = matchedOrder.Price;
				transaction.VolumeInUSD = realtime ? CalculateVolumeInUsd(matchedOrder.Volume, order.Instrument, transaction.CoreSide) : new double?();
				transaction.ExchangeMarkup = order.OpenCommission;
				transaction.CoreSpread = realtime ? spread : new double?();
				transaction.Comment = string.Empty;
				transaction.IsLive = realtime;

				await destination(transaction);

				index++;
			}
		}

		private double? CalculateExecutionDurationForOpen(IOrder order)
		{
			if (order.OpenDate.HasValue)
				return Math.Abs((order.OpenDate.Value - order.CreateDate).Milliseconds);
			return null;
		}

		private double? CalculateExecutionDurationForClose(IOrder order)
		{
			if (order.CloseDate.HasValue && order.StartClosingDate.HasValue)
				return Math.Abs((order.CloseDate.Value - order.StartClosingDate.Value).Milliseconds);
			return null;
		}

		private double? CalculateVolumeInUsd(double volume, string instrument, OrderDirection direction)
		{
			IMarginTradingAsset asset = _instrumentsCache.GetInstrumentById(instrument);

			if (asset.BaseAssetId == "USD")
			{
				return volume;
			}

			if (asset.QuoteAssetId == "USD")
			{
				InstrumentBidAskPair quote = _quoteCacheService.GetQuote(instrument);

				if (direction == OrderDirection.Buy)
				{
					return volume * quote.Ask;
				}

				return volume * quote.Bid;
			}

			return null;
		}

		private double CalculateSpread(string instrument)
		{
			InstrumentBidAskPair quote = _quoteCacheService.GetQuote(instrument);

			return quote.Ask - quote.Bid;
		}

		public async Task CreateTransactionsForOrderHistory(Func<Task<IEnumerable<IOrderHistory>>> source, Func<ITransaction, Task> destination)
		{
			foreach (IOrderHistory historyOrder in await source())
			{
				var order = MarginTradingOrderHistoryEntity.Restore(historyOrder);

				if (order == null || order.MatchedOrders == null || order.MatchedOrders.Count == 0 || (order.FillType == OrderFillType.FillOrKill && order.MatchedOrders.Sum(x => x.Volume) != order.Volume))
					continue;

				await CreateTransactionsForOpenOrderAsync(order, destination, false);

				if (order.Status == OrderStatus.Closed)
				{
					if (order.MatchedCloseOrders == null || order.MatchedCloseOrders.Count == 0 || (order.FillType == OrderFillType.FillOrKill && order.MatchedCloseOrders.Sum(x => x.Volume) != order.Volume))
						continue;

					await CreateTransactionsForClosedOrderAsync(order, destination, false);
				}
			}
		}
	}
}