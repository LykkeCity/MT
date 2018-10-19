using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    public class ValidateOrderService : IValidateOrderService
    {
        private readonly IQuoteCacheService _quoteCashService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly ITradingInstrumentsCacheService _tradingInstrumentsCache;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IDateService _dateService;
        private readonly MarginTradingSettings _marginSettings;
        private readonly ICfdCalculatorService _cfdCalculatorService;

        public ValidateOrderService(
            IQuoteCacheService quoteCashService,
            IAccountUpdateService accountUpdateService,
            IAccountsCacheService accountsCacheService,
            ITradingInstrumentsCacheService accountAssetsCacheService,
            IAssetPairsCache assetPairsCache,
            OrdersCache ordersCache,
            IAssetPairDayOffService assetDayOffService,
            IIdentityGenerator identityGenerator,
            IDateService dateService,
            MarginTradingSettings marginSettings,
            ICfdCalculatorService cfdCalculatorService)
        {
            _quoteCashService = quoteCashService;
            _accountUpdateService = accountUpdateService;
            _accountsCacheService = accountsCacheService;
            _tradingInstrumentsCache = accountAssetsCacheService;
            _assetPairsCache = assetPairsCache;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
            _marginSettings = marginSettings;
            _cfdCalculatorService = cfdCalculatorService;
        }
        
        
        #region Base validations

        public async Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndCreateOrders(
            OrderPlaceRequest request)
        {
            #region Validate properties
            
            if (request.Volume == 0)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume, "Volume can not be 0");
            }

            var assetPair = GetAssetPairIfAvailableForTrading(request.InstrumentId, request.Type.ToType<OrderType>(),
                request.ForceOpen, false);

            if (request.Type != OrderTypeContract.Market)
            {
                if (!request.Price.HasValue)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                        $"Price is required for {request.Type} order");
                }
                else
                {
                    request.Price = Math.Round(request.Price.Value, assetPair.Accuracy);

                    if (request.Price <= 0)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                            $"Price should be more then 0");
                    }
                }
            }
            else
            {
                //always ignore price for market orders
                request.Price = null;
            }

            var account = _accountsCacheService.TryGet(request.AccountId);

            if (account == null)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidAccount, "Account not found");
            }

            if (request.Validity.HasValue &&
                request.Type != OrderTypeContract.Market &&
                request.Validity.Value <= _dateService.Now())
            {
                throw new ValidateOrderException(OrderRejectReason.TechnicalError, "Invalid validity date");
            }
             
            try
            {
                _tradingInstrumentsCache.GetTradingInstrument(account.TradingConditionId, assetPair.Id);
            }
            catch
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidInstrument,
                    "Instrument is not available for trading on selected account");
            }
            
            var equivalentSettings =
                _marginSettings.ReportingEquivalentPricesSettings.FirstOrDefault(x => x.LegalEntity == account.LegalEntity);
			
            if(string.IsNullOrEmpty(equivalentSettings?.EquivalentAsset))
                throw new Exception($"No reporting equivalent prices asset found for legalEntity: {account.LegalEntity}");

            //set special account-quote instrument
//            if (_assetPairsCache.TryGetAssetPairQuoteSubst(order.AccountAssetId, order.Instrument,
//                    order.LegalEntity, out var substAssetPair))
//            {
//                order.MarginCalcInstrument = substAssetPair.Id;
//            }
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                request.CorrelationId = _identityGenerator.GenerateGuid();
            }

            #endregion

            if (request.Type == OrderTypeContract.StopLoss ||
                request.Type == OrderTypeContract.TakeProfit ||
                request.Type == OrderTypeContract.TrailingStop)
            {
                var order = await ValidateAndGetSlOrTpOrder(request, request.Type, request.Price, equivalentSettings,
                    null);

                return (order, new List<Order>());
            }

            var initialParameters = await GetOrderInitialParameters(request.InstrumentId, account.LegalEntity,
                equivalentSettings, account.BaseAssetId);

            var volume = request.Direction == OrderDirectionContract.Sell ? -Math.Abs(request.Volume)
                : request.Direction == OrderDirectionContract.Buy ? Math.Abs(request.Volume)
                : request.Volume;

            var originator = GetOriginator(request.Originator);

            var baseOrder = new Order(initialParameters.id, initialParameters.code, request.InstrumentId, volume,
                initialParameters.now, initialParameters.now, request.Validity, account.Id,
                account.TradingConditionId, account.BaseAssetId, request.Price, equivalentSettings.EquivalentAsset,
                OrderFillType.FillOrKill, string.Empty, account.LegalEntity, request.ForceOpen,
                request.Type.ToType<OrderType>(), request.ParentOrderId, request.PositionId, originator,
                initialParameters.equivalentPrice, initialParameters.fxPrice, OrderStatus.Placed,
                request.AdditionalInfo, request.CorrelationId);

            ValidateBaseOrderPrice(baseOrder, baseOrder.Price);

            var relatedOrders = new List<Order>();

            if (request.StopLoss.HasValue)
            {
                request.StopLoss = Math.Round(request.StopLoss.Value, assetPair.Accuracy);

                if (request.StopLoss <= 0)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                        $"StopLoss should be more then 0");
                }

                var orderType = request.UseTrailingStop ? OrderTypeContract.TrailingStop : OrderTypeContract.StopLoss;
                
                var sl = await ValidateAndGetSlOrTpOrder(request, orderType, request.StopLoss,
                    equivalentSettings, baseOrder);
                
                if (sl != null)
                    relatedOrders.Add(sl);    
            }
            
            if (request.TakeProfit.HasValue)
            {
                request.TakeProfit = Math.Round(request.TakeProfit.Value, assetPair.Accuracy);

                if (request.TakeProfit <= 0)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                        $"TakeProfit should be more then 0");
                }

                var tp = await ValidateAndGetSlOrTpOrder(request, OrderTypeContract.TakeProfit, request.TakeProfit,
                    equivalentSettings, baseOrder);
                
                if (tp != null)
                    relatedOrders.Add(tp);    
            }
            
            return (baseOrder, relatedOrders);
        }
        
        public void ValidateOrderPriceChange(Order order, decimal newPrice)
        {
            if (order.IsBasicOrder())
            {
                ValidateBaseOrderPrice(order, newPrice);
                
                var relatedOrders = GetRelatedOrders(order);

                ValidateBaseOrderPriceAgainstRelated(order, relatedOrders, newPrice);
            }
            else
            {
                ValidateRelatedOrderPriceAgainstBase(order, newPrice: newPrice);
            }
        }
        
        private async Task<Order> ValidateAndGetSlOrTpOrder(OrderPlaceRequest request, OrderTypeContract type,
            decimal? price, ReportingEquivalentPricesSettings equivalentSettings, Order parentOrder)
        {
            var orderType = type.ToType<OrderType>();

            Order order = null;
            
            if (parentOrder == null)
            {
                if (!string.IsNullOrEmpty(request.ParentOrderId))
                {
                    parentOrder = _ordersCache.GetOrderById(request.ParentOrderId);
                }
            }

            if (parentOrder != null)
            {
                ValidateRelatedOrderAlreadyExists(parentOrder.RelatedOrders, orderType);
                
                var initialParameters = await GetOrderInitialParameters(parentOrder.AssetPairId,
                    parentOrder.LegalEntity, equivalentSettings, parentOrder.AccountAssetId);

                var originator = GetOriginator(request.Originator);
                
                order = new Order(initialParameters.id, initialParameters.code, parentOrder.AssetPairId,
                    -parentOrder.Volume, initialParameters.now, initialParameters.now,
                    request.Validity, parentOrder.AccountId, parentOrder.TradingConditionId, parentOrder.AccountAssetId,
                    price, parentOrder.EquivalentAsset, OrderFillType.FillOrKill, string.Empty,
                    parentOrder.LegalEntity, false, orderType, parentOrder.Id, null,
                    originator, initialParameters.equivalentPrice,
                    initialParameters.fxPrice, OrderStatus.Placed, request.AdditionalInfo, request.CorrelationId);
            } 
            else if (!string.IsNullOrEmpty(request.PositionId))
            {
                var position = _ordersCache.Positions.GetPositionById(request.PositionId);

                ValidateRelatedOrderAlreadyExists(position.RelatedOrders, orderType);

                var initialParameters = await GetOrderInitialParameters(position.AssetPairId,
                    position.LegalEntity, equivalentSettings, position.AccountAssetId);

                var originator = GetOriginator(request.Originator);

                order = new Order(initialParameters.id, initialParameters.code, position.AssetPairId,
                    -position.Volume, initialParameters.now, initialParameters.now,
                    request.Validity, position.AccountId, position.TradingConditionId, position.AccountAssetId,
                    price, position.EquivalentAsset, OrderFillType.FillOrKill, string.Empty,
                    position.LegalEntity, false, orderType, null, position.Id,
                    originator, initialParameters.equivalentPrice,
                    initialParameters.fxPrice, OrderStatus.Placed, request.AdditionalInfo, request.CorrelationId);
            }

            if (order == null)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidParent,
                    "Related order must have parent order or position");
            }

            ValidateRelatedOrderPriceAgainstBase(order, parentOrder, order.Price);

            return order;
        }

        private static void ValidateRelatedOrderAlreadyExists(List<RelatedOrderInfo> relatedOrders, OrderType orderType)
        {
            if ((orderType == OrderType.TakeProfit
                 && relatedOrders.Any(o => o.Type == OrderType.TakeProfit))
                || ((orderType == OrderType.StopLoss || orderType == OrderType.TrailingStop)
                    && relatedOrders.Any(o => o.Type == OrderType.StopLoss || o.Type == OrderType.TrailingStop)))
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidParent,
                    $"Parent order already has related order with type {orderType}");
            }
        }

        private async Task<(string id, long code, DateTime now, decimal equivalentPrice, decimal fxPrice)>
            GetOrderInitialParameters(string assetPairId, string legalEntity,
                ReportingEquivalentPricesSettings equivalentSettings, string accountAssetId)
        {
            var id = _identityGenerator.GenerateAlphanumericId();
            var code = await _identityGenerator.GenerateIdAsync(nameof(Order));
            var now = _dateService.Now();
            var equivalentPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(equivalentSettings.EquivalentAsset,
                assetPairId, legalEntity);
            var fxPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(accountAssetId,
                assetPairId, legalEntity);
            return (id, code, now, equivalentPrice, fxPrice);
        }

        private void ValidateBaseOrderPrice(Order order, decimal? orderPrice)
        {
            if (!_quoteCashService.TryGetQuoteById(order.AssetPairId, out var quote))
            {
                throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "Quote not found");
            }

            //TODO: implement in MTC-155            
//            if (_assetDayOffService.ArePendingOrdersDisabled(order.AssetPairId))
//            {
//                throw new ValidateOrderException(OrderRejectReason.NoLiquidity,
//                    "Trades for instrument are not available");
//            }

            if (order.OrderType != OrderType.Stop)
                return;

            if (order.Direction == OrderDirection.Buy && quote.Ask >= orderPrice)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                    string.Format(MtMessages.Validation_PriceAboveAsk, orderPrice, quote.Ask),
                    $"{order.AssetPairId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
            } 
            
            if (order.Direction == OrderDirection.Sell && quote.Bid <= orderPrice )
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice, 
                    string.Format(MtMessages.Validation_PriceBelowBid, orderPrice, quote.Bid),
                    $"{order.AssetPairId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
            }
        }

        private void ValidateBaseOrderPriceAgainstRelated(Order baseOrder, List<Order> relatedOrders, decimal newPrice)
        {
            //even if price is defined for market - ignore it
            if (baseOrder.OrderType == OrderType.Market)
                return;

            var slPrice = relatedOrders
                .FirstOrDefault(o => o.OrderType == OrderType.StopLoss || o.OrderType == OrderType.TrailingStop)?.Price;

            var tpPrice = relatedOrders
                .FirstOrDefault(o => o.OrderType == OrderType.TakeProfit)?.Price;

            if (baseOrder.Direction == OrderDirection.Buy &&
                (slPrice.HasValue && slPrice > newPrice
                 || tpPrice.HasValue && tpPrice < newPrice)
                ||
                baseOrder.Direction == OrderDirection.Sell &&
                (slPrice.HasValue && slPrice < newPrice
                 || tpPrice.HasValue && tpPrice > newPrice))
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                    "Price is not valid against related orders prices.");
            }
        }

        private void ValidateRelatedOrderPriceAgainstBase(Order relatedOrder, Order baseOrder = null, decimal? newPrice = null)
        {
            if (newPrice == null)
                newPrice = relatedOrder.Price;

            decimal basePrice;

            if ((baseOrder != null ||
                string.IsNullOrEmpty(relatedOrder.ParentPositionId) && 
                !string.IsNullOrEmpty(relatedOrder.ParentOrderId) && 
                _ordersCache.TryGetOrderById(relatedOrder.ParentOrderId, out baseOrder)) &&
                baseOrder.Price.HasValue)
            {
                basePrice = baseOrder.Price.Value;
            }
            else
            {
                if (!_quoteCashService.TryGetQuoteById(relatedOrder.AssetPairId, out var quote))
                {
                    throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "Quote not found");
                }

                basePrice = quote.GetPriceForOrderDirection(relatedOrder.Direction);
            }

            switch (relatedOrder.OrderType)
            {
                case OrderType.StopLoss:
                case OrderType.TrailingStop:
                    ValidateStopLossOrderPrice(relatedOrder.Direction, newPrice, basePrice);
                    break;
                
                case OrderType.TakeProfit:
                    ValidateTakeProfitOrderPrice(relatedOrder.Direction, newPrice, basePrice);
                    break;
            }
        }
        
        private void ValidateTakeProfitOrderPrice(OrderDirection orderDirection, decimal? orderPrice, decimal basePrice)
        {
            if (orderDirection == OrderDirection.Buy && basePrice <= orderPrice)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                    string.Format(MtMessages.Validation_TakeProfitMustBeLess, orderPrice, basePrice));
            }
            
            if (orderDirection == OrderDirection.Sell && basePrice >= orderPrice)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                    string.Format(MtMessages.Validation_TakeProfitMustBeMore, orderPrice, basePrice));
            }
        }
        
        private void ValidateStopLossOrderPrice(OrderDirection orderDirection, decimal? orderPrice, decimal basePrice)
        {
            if (orderDirection == OrderDirection.Buy && basePrice >= orderPrice)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                    string.Format(MtMessages.Validation_StopLossMustBeMore, orderPrice, basePrice));
            }
            
            if (orderDirection == OrderDirection.Sell && basePrice <= orderPrice)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                    string.Format(MtMessages.Validation_StopLossMustBeLess, orderPrice, basePrice));
            }
        }
      
        private OriginatorType GetOriginator(OriginatorTypeContract? originator)
        {
            if (originator == null || originator.Value == default(OriginatorTypeContract))
            {
                return OriginatorType.Investor;
            }

            return originator.ToType<OriginatorType>();
        }
        
        #endregion

        
        #region Pre-trade validations
        
        public void MakePreTradeValidation(Order order, bool shouldOpenNewPosition)
        {
            GetAssetPairIfAvailableForTrading(order.AssetPairId, order.OrderType, shouldOpenNewPosition, true);

            ValidateTradeLimits(order.AssetPairId, order.TradingConditionId, order.AccountId, order.Volume);

            if (shouldOpenNewPosition)
                ValidateMargin(order);

        }
        
        private IAssetPair GetAssetPairIfAvailableForTrading(string assetPairId, OrderType orderType, 
            bool shouldOpenNewPosition, bool isPreTradeValidation)
        {
            if (isPreTradeValidation || orderType == OrderType.Market)
            {
                if (_assetDayOffService.IsDayOff(assetPairId))
                {
                    throw new ValidateOrderException(OrderRejectReason.NoLiquidity,
                        "Trades for instrument are not available");
                }
            }
            else if (_assetDayOffService.ArePendingOrdersDisabled(assetPairId))
            {
                throw new ValidateOrderException(OrderRejectReason.NoLiquidity,
                    "Pending orders for instrument are not available");
            }

            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(assetPairId); 
            
            if (assetPair == null)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, "Instrument not found");
            }
            
            if (assetPair.IsDiscontinued)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, 
                    "Trading for the instrument is discontinued");
            }

            if (assetPair.IsSuspended)
            {
                if (isPreTradeValidation)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, 
                        "Orders execution for instrument is temporarily unavailable");
                }
               
                if (orderType == OrderType.Market)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, 
                        "Market orders for instrument are temporarily unavailable");
                }
            } 
            
            if (assetPair.IsFrozen && shouldOpenNewPosition)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, 
                    "Opening new positions is temporarily unavailable");
            }

            return assetPair;
        }

        private void ValidateTradeLimits(string assetPairId, string tradingConditionId, string accountId, decimal volume)
        {
            var tradingInstrument =
                _tradingInstrumentsCache.GetTradingInstrument(tradingConditionId, assetPairId);

            if (tradingInstrument.DealMaxLimit > 0 && Math.Abs(volume) > tradingInstrument.DealMaxLimit)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume,
                    $"Margin Trading is in beta testing. The volume of a single order is temporarily limited to {tradingInstrument.DealMaxLimit} {tradingInstrument.Instrument}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
            }

            var existingPositionsVolume = _ordersCache.Positions.GetPositionsByInstrumentAndAccount(assetPairId, accountId)
                .Sum(o => o.Volume);

            if (tradingInstrument.PositionLimit > 0 &&
                Math.Abs(existingPositionsVolume + volume) > tradingInstrument.PositionLimit)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume,
                    $"Margin Trading is in beta testing. The volume of the net open position is temporarily limited to {tradingInstrument.PositionLimit} {tradingInstrument.Instrument}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
            }
        }
        
        private void ValidateMargin(Order order)
        {
            if (!_accountUpdateService.IsEnoughBalance(order))
            {
                var account = _accountsCacheService.Get(order.AccountId);
                
                throw new ValidateOrderException(OrderRejectReason.NotEnoughBalance, MtMessages.Validation_NotEnoughBalance, $"Account available balance is {account.GetTotalCapital()}");
            }
        }

        private List<Order> GetRelatedOrders(Order baseOrder)
        {
            var result = new List<Order>();
            
            foreach (var relatedOrderInfo in baseOrder.RelatedOrders)
            {
                if (_ordersCache.TryGetOrderById(relatedOrderInfo.Id, out var relatedOrder))
                {
                    result.Add(relatedOrder);
                }
            }

            return result;
        }
        
        #endregion
        
    }
}
