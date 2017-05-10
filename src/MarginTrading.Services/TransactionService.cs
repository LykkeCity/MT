using MarginTrading.AzureRepositories;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IInstrumentsCache _instrumentsCache;

        public TransactionService(IInstrumentsCache instrumentsCache, IQuoteCacheService quoteCacheService)
        {
            _instrumentsCache = instrumentsCache;
            _quoteCacheService = quoteCacheService;
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

                transaction.OrderId = $"{order.Id}_Close";
                transaction.TakerOrderId = order.Id;
                transaction.TakerLykkeId = !string.IsNullOrWhiteSpace(order.ClientId) ? order.ClientId : "AnnonymousTrader";
                transaction.TakerAccountId = order.AccountId;
                transaction.TakerAction = TakerAction.Cancel;
                transaction.TakerSpread = realtime ? spread / 2 : new double?();

                transaction.MakerOrderId = matchedOrder.OrderId;
                transaction.MakerLykkeId = !string.IsNullOrWhiteSpace(matchedOrder.ClientId) ? matchedOrder.ClientId : "Lykke";
                transaction.MakerSpread = realtime ? spread / 2 : new double?();

                transaction.LykkeExecutionId = $"{order.Id}_{matchedOrder.OrderId}_Cancel";
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

                await destination(transaction);
            }
        }

        public async Task CreateTransactionsForClosedOrderAsync(IOrder order, Func<ITransaction, Task> destination, bool realtime = true)
        {
            foreach (MatchedOrder matchedOrder in order.MatchedCloseOrders)
            {
                var transaction = new Transaction();

                double? spread = null;

                if (realtime)
                {
                    CalculateSpread(order.Instrument);
                }

                transaction.OrderId = $"{order.Id}_Close";
                transaction.TakerOrderId = order.Id;
                transaction.TakerLykkeId = !string.IsNullOrWhiteSpace(order.ClientId) ? order.ClientId : "AnnonymousTrader";
                transaction.TakerAccountId = order.AccountId;
                transaction.TakerAction = TakerAction.Close;
                transaction.TakerSpread = realtime ? spread / 2 : new double?();

                transaction.MakerOrderId = matchedOrder.OrderId;
                transaction.MakerLykkeId = !string.IsNullOrWhiteSpace(matchedOrder.ClientId) ? matchedOrder.ClientId : "Lykke";
                transaction.MakerSpread = realtime ? spread / 2 : new double?();

                transaction.LykkeExecutionId = $"{order.Id}_{matchedOrder.OrderId}_Close";
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

                await destination(transaction);
            }
        }

        public async Task CreateTransactionsForOpenOrderAsync(IOrder order, Func<ITransaction, Task> destination, bool realtime = true)
        {
            foreach (MatchedOrder matchedOrder in order.MatchedOrders)
            {
                var transaction = new Transaction();

                double? spread = null;

                if (realtime)
                {
                    spread = CalculateSpread(order.Instrument);
                }

                transaction.OrderId = $"{order.Id}_Open";
                transaction.TakerOrderId = order.Id;
                transaction.TakerLykkeId = !string.IsNullOrWhiteSpace(order.ClientId) ? order.ClientId : "AnnonymousTrader";
                transaction.TakerAccountId = order.AccountId;
                transaction.TakerAction = TakerAction.Open;
                transaction.TakerSpread = realtime ? spread / 2 : new double?();

                transaction.MakerOrderId = matchedOrder.OrderId;
                transaction.MakerLykkeId = !string.IsNullOrWhiteSpace(matchedOrder.ClientId) ? order.ClientId : "Lykke";
                transaction.MakerSpread = realtime ? spread / 2 : new double?();

                transaction.LykkeExecutionId = $"{order.Id}_{matchedOrder.OrderId}_Open";
                transaction.CoreSide = order.GetOrderType();
                transaction.CoreSymbol = order.Instrument;
                transaction.ExecutedTime = order.OpenDate;
                transaction.ExecutionDuration = CalculateExecutionDurationForOpen(order);
                transaction.FilledVolume = matchedOrder.Volume;
                transaction.Price = matchedOrder.Price;
                transaction.VolumeInUSD = realtime ? CalculateVolumeInUsd(matchedOrder.Volume, order.Instrument, transaction.CoreSide) : new double?();
                transaction.ExchangeMarkup = 0;
                transaction.CoreSpread = realtime ? spread : new double?();
                transaction.Comment = string.Empty;
                transaction.IsLive = realtime;

                await destination(transaction);
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
            if (asset == null)
            {

            }

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

                await CreateTransactionsForOpenOrderAsync(order, destination, false);

                await CreateTransactionsForClosedOrderAsync(order, destination, false);
            }
        }
    }
}