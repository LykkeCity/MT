using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class SnapshotService : ISnapshotService
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly IOrderReader _orderReader;
        private readonly IDateService _dateService;

        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;
        
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public SnapshotService(
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IAccountsCacheService accountsCacheService,
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            IOrderReader orderReader,
            IDateService dateService,
            ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _accountsCacheService = accountsCacheService;
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _orderReader = orderReader;
            _dateService = dateService;
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
        }

        public async Task<string> MakeTradingDataSnapshot(string correlationId)
        {
            if (_scheduleSettingsCacheService.GetPlatformTradingEnabled())
            {
                throw new Exception(
                    "Trading should be stopped for whole platform in order to make trading data snapshot.");
            }

            await _semaphoreSlim.WaitAsync();

            try
            {
                var orders = _orderReader.GetAllOrders();
                var positions = _orderReader.GetPositions();
                var accountStats = _accountsCacheService.GetAll();
                var bestFxPrices = _fxRateCacheService.GetAllQuotes();
                var bestPrices = _quoteCacheService.GetAllQuotes();

                await _tradingEngineSnapshotsRepository.Add(
                    correlationId, 
                    _dateService.Now(), 
                    orders.Select(x => x.ConvertToContract(GetRelatedOrders(x))), 
                    positions.Select(Convert), 
                    accountStats.Select(Convert), 
                    bestFxPrices.ToDictionary(q => q.Key, q => Convert(q.Value)),
                    bestPrices.ToDictionary(q => q.Key, q => Convert(q.Value)));

                return $@"Trading data snapshot was written to the storage. 
Orders: {orders.Length}, positions: {positions.Length}, accounts: {accountStats.Count}, 
best FX prices: {bestFxPrices.Count}, best trading prices: {bestPrices.Count}.";
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private List<Order> GetRelatedOrders(Order order)
        {
            var relatedOrders = new List<Order>();

            foreach (var relatedOrderInfo in order.RelatedOrders)
            {
                if (_orderReader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrders.Add(relatedOrder);
                }
            }

            return relatedOrders;
        }
        
        private OpenPositionContract Convert(Position position)
        {
            var relatedOrders = new List<RelatedOrderInfoContract>();

            foreach (var relatedOrderInfo in position.RelatedOrders)
            {
                if (_orderReader.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    relatedOrders.Add(new RelatedOrderInfoContract
                    {
                        Id = relatedOrder.Id,
                        Price = relatedOrder.Price ?? 0,
                        Type = relatedOrder.OrderType.ToType<OrderTypeContract>(),
                        Status = relatedOrder.Status.ToType<OrderStatusContract>(),
                        ModifiedTimestamp = relatedOrder.LastModified
                    });
                }
            }

            return new OpenPositionContract
            {
                AccountId = position.AccountId,
                AssetPairId = position.AssetPairId,
                CurrentVolume = position.Volume,
                Direction = position.Direction.ToType<PositionDirectionContract>(),
                Id = position.Id,
                OpenPrice = position.OpenPrice,
                OpenFxPrice = position.OpenFxPrice,
                ClosePrice = position.ClosePrice,
                ExpectedOpenPrice = position.ExpectedOpenPrice,
                OpenTradeId = position.OpenTradeId,
                OpenOrderType = position.OpenOrderType.ToType<OrderTypeContract>(),
                OpenOrderVolume = position.OpenOrderVolume,
                PnL = position.GetFpl(),
                ChargedPnl = position.ChargedPnL,
                Margin = position.GetMarginMaintenance(),
                FxRate = position.GetFplRate(),
                FxAssetPairId = position.FxAssetPairId,
                FxToAssetPairDirection = position.FxToAssetPairDirection.ToType<FxToAssetPairDirectionContract>(),
                RelatedOrders = position.RelatedOrders.Select(o => o.Id).ToList(),
                RelatedOrderInfos = relatedOrders,
                OpenTimestamp = position.OpenDate,
                ModifiedTimestamp = position.LastModified,
                TradeId = position.Id,
                AdditionalInfo = position.AdditionalInfo
            };
        }
        
        private static AccountStatContract Convert(IMarginTradingAccount item)
        {
            return new AccountStatContract
            {
                AccountId = item.Id,
                BaseAssetId = item.BaseAssetId,
                Balance = item.Balance,
                MarginCallLevel = item.GetMarginCall1Level(),
                StopOutLevel = item.GetStopOutLevel(),
                TotalCapital = item.GetTotalCapital(),
                FreeMargin = item.GetFreeMargin(),
                MarginAvailable = item.GetMarginAvailable(),
                UsedMargin = item.GetUsedMargin(),
                CurrentlyUsedMargin = item.GetCurrentlyUsedMargin(),
                InitiallyUsedMargin = item.GetInitiallyUsedMargin(),
                MarginInit = item.GetMarginInit(),
                PnL = item.GetPnl(),
                UnrealizedDailyPnl = item.GetUnrealizedDailyPnl(),
                OpenPositionsCount = item.GetOpenPositionsCount(),
                ActiveOrdersCount = item.GetActiveOrdersCount(),
                MarginUsageLevel = item.GetMarginUsageLevel(),
                LegalEntity = item.LegalEntity,
                IsInLiquidation = item.IsInLiquidation(),
                MarginNotificationLevel = item.GetAccountLevel().ToString()
            };
        }
        
        private static BestPriceContract Convert(InstrumentBidAskPair arg)
        {
            return new BestPriceContract
            {
                Ask = arg.Ask,
                Bid = arg.Bid,
                Id = arg.Instrument,
                Timestamp = arg.Date,
            };
        }
    }
}