// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using Microsoft.FeatureManagement;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace MarginTrading.Backend.Services
{
    public class OrderValidator : IOrderValidator
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
        private readonly IFeatureManager _featureManager;

        public OrderValidator(
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
            ICfdCalculatorService cfdCalculatorService, 
            IFeatureManager featureManager)
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
            _featureManager = featureManager;
        }
        
        
        #region Base validations
        
        public async Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndCreateOrders(
            OrderPlaceRequest request)
        {
            #region Validate properties
            
            if (request.Volume == 0)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidVolume, "Volume can not be 0");
            }

            var assetPair = GetAssetPairIfAvailableForTrading(request.InstrumentId, request.Type.ToType<OrderType>(),
                request.ForceOpen, false);

            if (request.Type != OrderTypeContract.Market || request.AdditionalInfo.IsCancellationTrade(out _))
            {
                if (!request.Price.HasValue)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice,
                        $"Price is required for {request.Type} order");
                }
                else
                {
                    request.Price = Math.Round(request.Price.Value, assetPair.Accuracy);

                    if (request.Price <= 0)
                    {
                        throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice,
                            $"Price should be more than 0");
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
                throw new OrderRejectionException(OrderRejectReason.InvalidAccount, "Account not found");
            }

            ValidateValidity(request.Validity, request.Type.ToType<OrderType>());

            ITradingInstrument tradingInstrument;
            try
            {
                tradingInstrument =
                    _tradingInstrumentsCache.GetTradingInstrument(account.TradingConditionId, assetPair.Id);
            }
            catch
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                    $"Instrument {assetPair.Id} is not available for trading on selected account");
            }

            if (tradingInstrument.DealMinLimit > 0 && Math.Abs(request.Volume) < tradingInstrument.DealMinLimit)
            {
                throw new OrderRejectionException(OrderRejectReason.MinOrderSizeLimit,
                    $"The minimum volume of a single order is limited to {tradingInstrument.DealMinLimit} {tradingInstrument.Instrument}.");
            }

            #endregion

            var equivalentSettings = GetReportingEquivalentPricesSettings(account.LegalEntity);

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

            var baseOrder = new Order(initialParameters.Id, initialParameters.Code, request.InstrumentId, volume,
                initialParameters.Now, initialParameters.Now, request.Validity, account.Id, account.TradingConditionId,
                account.BaseAssetId, request.Price, equivalentSettings.EquivalentAsset, OrderFillType.FillOrKill,
                string.Empty, account.LegalEntity, request.ForceOpen, request.Type.ToType<OrderType>(),
                request.ParentOrderId, null, originator, initialParameters.EquivalentPrice,
                initialParameters.FxPrice, initialParameters.FxAssetPairId, initialParameters.FxToAssetPairDirection,
                OrderStatus.Placed, request.AdditionalInfo);

            ValidateBaseOrderPrice(baseOrder, baseOrder.Price);

            var relatedOrders = new List<Order>();

            if (request.StopLoss.HasValue)
            {
                request.StopLoss = Math.Round(request.StopLoss.Value, assetPair.Accuracy);

                if (request.StopLoss <= 0)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidStoploss,
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
                    throw new OrderRejectionException(OrderRejectReason.InvalidTakeProfit,
                        $"TakeProfit should be more then 0");
                }

                var tp = await ValidateAndGetSlOrTpOrder(request, OrderTypeContract.TakeProfit, request.TakeProfit,
                    equivalentSettings, baseOrder);
                
                if (tp != null)
                    relatedOrders.Add(tp);    
            }

            await ValidateProductComplexityConfirmation(request, account);
            
            return (baseOrder, relatedOrders);
        }

        private async Task ValidateProductComplexityConfirmation(OrderPlaceRequest order, MarginTradingAccount account)
        {
            if (!await _featureManager.IsEnabledAsync(BrokerFeature.ProductComplexityWarning))
            {
                return;
            }

            var isBasicOrder = new[]
                {
                    OrderTypeContract.Market,
                    OrderTypeContract.Limit,
                    OrderTypeContract.Stop
                }
                .Contains(order.Type);

            
            if (!isBasicOrder)
            {
                return;
            }

            var shouldShowProductComplexityWarning = AccountAdditionalInfoExtensions.Deserialize(account.AdditionalInfo).ShouldShowProductComplexityWarning ?? true;
            if (!shouldShowProductComplexityWarning)
            {
                return;
            }

            var productComplexityConfimationReceived = order.AdditionalInfo.ProductComplexityConfirmationReceived();

            if (!productComplexityConfimationReceived)
            {
                throw new OrderRejectionException(OrderRejectReason.AccountInvalidState,
                    $"Product complexity warning not received for order, placed by account {account.Id}");
            }
        }

        public void ValidateForceOpenChange(Order order, bool? forceOpen)
        {
            if (!forceOpen.HasValue || forceOpen.Value == order.ForceOpen)
            {
                return;
            }
            
            if (!order.IsBasicPendingOrder() && forceOpen.Value)
            {
                throw new OrderRejectionException(OrderRejectReason.None,
                    "Force open cannot be set to true for related order");
            }

            if (forceOpen.Value && order.Direction == OrderDirection.Sell)
            {
                var tradingInstrument =
                    _tradingInstrumentsCache.GetTradingInstrument(order.TradingConditionId, order.AssetPairId);
                if (!tradingInstrument.ShortPosition)
                {
                    throw new OrderRejectionException(OrderRejectReason.ShortPositionsDisabled,
                        $"Short positions are disabled for {tradingInstrument.Instrument}.");
                }
            }
        }

        public void ValidateValidity(DateTime? validity, OrderType orderType)
        {
            if (validity.HasValue &&
                orderType != OrderType.Market &&
                validity.Value.Date < _dateService.Now().Date)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidValidity, "Invalid validity date");
            }
        }

        public void ValidateOrderPriceChange(Order order, decimal newPrice)
        {
            if (order.Price == newPrice)
                return;

            if (newPrice <= 0)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice,
                    $"Price should be more than 0");
            }
            
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

        public async Task<OrderInitialParameters> GetOrderInitialParameters(string assetPairId, string accountId)
        {
            var account = _accountsCacheService.Get(accountId);

            var equivalentPricesSettings = GetReportingEquivalentPricesSettings(account.LegalEntity);

            return await GetOrderInitialParameters(assetPairId, account.LegalEntity, equivalentPricesSettings,
                account.BaseAssetId);
        }

        private ReportingEquivalentPricesSettings GetReportingEquivalentPricesSettings(string legalEntity)
        {
            var equivalentPricesSettings =
                _marginSettings.ReportingEquivalentPricesSettings.FirstOrDefault(x => x.LegalEntity == legalEntity);

            if (string.IsNullOrEmpty(equivalentPricesSettings?.EquivalentAsset))
            {
                throw new Exception($"No reporting equivalent prices asset found for legalEntity: {legalEntity}");
            }

            return equivalentPricesSettings;
        }

        private async Task<Order> ValidateAndGetSlOrTpOrder(OrderPlaceRequest request, OrderTypeContract type,
            decimal? price, ReportingEquivalentPricesSettings equivalentSettings, Order parentOrder)
        {
            var orderType = type.ToType<OrderType>();
            Order order = null;
            var placedAltogether = parentOrder != null;
            
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

                order = new Order(initialParameters.Id, initialParameters.Code, parentOrder.AssetPairId,
                    -parentOrder.Volume, initialParameters.Now, initialParameters.Now, 
                    placedAltogether ? null : request.Validity,
                    parentOrder.AccountId, parentOrder.TradingConditionId, parentOrder.AccountAssetId, price,
                    parentOrder.EquivalentAsset, OrderFillType.FillOrKill, string.Empty, parentOrder.LegalEntity, false,
                    orderType, parentOrder.Id, null, originator, initialParameters.EquivalentPrice,
                    initialParameters.FxPrice, initialParameters.FxAssetPairId,
                    initialParameters.FxToAssetPairDirection, OrderStatus.Placed, request.AdditionalInfo);
            } 
            else if (!string.IsNullOrEmpty(request.PositionId))
            {
                var position = _ordersCache.Positions.GetPositionById(request.PositionId);

                ValidateRelatedOrderAlreadyExists(position.RelatedOrders, orderType);

                var initialParameters = await GetOrderInitialParameters(position.AssetPairId,
                    position.LegalEntity, equivalentSettings, position.AccountAssetId);

                var originator = GetOriginator(request.Originator);

                order = new Order(initialParameters.Id, initialParameters.Code, position.AssetPairId, -position.Volume,
                    initialParameters.Now, initialParameters.Now, request.Validity, position.AccountId,
                    position.TradingConditionId, position.AccountAssetId, price, position.EquivalentAsset,
                    OrderFillType.FillOrKill, string.Empty, position.LegalEntity, false, orderType, null, position.Id,
                    originator, initialParameters.EquivalentPrice, initialParameters.FxPrice,
                    initialParameters.FxAssetPairId, initialParameters.FxToAssetPairDirection, OrderStatus.Placed,
                    request.AdditionalInfo);
            }

            if (order == null)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidParent,
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
                throw new OrderRejectionException(OrderRejectReason.InvalidParent,
                    $"Parent order already has related order with type {orderType}");
            }
        }

        private async Task<OrderInitialParameters> GetOrderInitialParameters(string assetPairId, string legalEntity,
            ReportingEquivalentPricesSettings equivalentSettings, string accountAssetId)
        {
            var fxAssetPairIdAndDirection = _cfdCalculatorService.GetFxAssetPairIdAndDirection(accountAssetId, 
                assetPairId, legalEntity);
            
            return new OrderInitialParameters
            {
                Id = AlphanumericIdentityGenerator.GenerateAlphanumericId(),
                Code = await _identityGenerator.GenerateIdAsync(nameof(Order)),
                Now = _dateService.Now(),
                EquivalentPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(equivalentSettings.EquivalentAsset,
                    assetPairId, legalEntity),
                FxPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(accountAssetId,
                    assetPairId, legalEntity),
                FxAssetPairId = fxAssetPairIdAndDirection.id,
                FxToAssetPairDirection = fxAssetPairIdAndDirection.direction,
            };
        }

        private void ValidateBaseOrderPrice(Order order, decimal? orderPrice)
        {
            if (!_quoteCashService.TryGetQuoteById(order.AssetPairId, out var quote))
            {
                throw new OrderRejectionException(OrderRejectReason.NoLiquidity, "Quote not found");
            }

            if (order.OrderType == OrderType.Limit)
            {
                if (order.Direction == OrderDirection.Buy && quote.Ask <= orderPrice)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice,
                        string.Format(MtMessages.Validation_PriceAboveAsk, orderPrice, quote.Ask),
                        $"{order.AssetPairId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
                } 
            
                if (order.Direction == OrderDirection.Sell && quote.Bid >= orderPrice)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice, 
                        string.Format(MtMessages.Validation_PriceBelowBid, orderPrice, quote.Bid),
                        $"{order.AssetPairId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
                }
            }

            if (order.OrderType == OrderType.Stop)
            {
                if (order.Direction == OrderDirection.Buy && quote.Ask >= orderPrice)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice,
                        string.Format(MtMessages.Validation_PriceBelowAsk, orderPrice, quote.Ask),
                        $"{order.AssetPairId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
                } 
            
                if (order.Direction == OrderDirection.Sell && quote.Bid <= orderPrice)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice, 
                        string.Format(MtMessages.Validation_PriceAboveBid, orderPrice, quote.Bid),
                        $"{order.AssetPairId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
                }
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
                (slPrice.HasValue && slPrice >= newPrice
                 || tpPrice.HasValue && tpPrice <= newPrice)
                ||
                baseOrder.Direction == OrderDirection.Sell &&
                (slPrice.HasValue && slPrice <= newPrice
                 || tpPrice.HasValue && tpPrice >= newPrice))
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidExpectedOpenPrice,
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
                    throw new OrderRejectionException(OrderRejectReason.NoLiquidity, "Quote not found");
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
                throw new OrderRejectionException(OrderRejectReason.InvalidTakeProfit,
                    string.Format(MtMessages.Validation_TakeProfitMustBeLess, orderPrice, basePrice));
            }
            
            if (orderDirection == OrderDirection.Sell && basePrice >= orderPrice)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidTakeProfit,
                    string.Format(MtMessages.Validation_TakeProfitMustBeMore, orderPrice, basePrice));
            }
        }
        
        private void ValidateStopLossOrderPrice(OrderDirection orderDirection, decimal? orderPrice, decimal basePrice)
        {
            if (orderDirection == OrderDirection.Buy && basePrice >= orderPrice)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidStoploss,
                    string.Format(MtMessages.Validation_StopLossMustBeMore, orderPrice, basePrice));
            }
            
            if (orderDirection == OrderDirection.Sell && basePrice <= orderPrice)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidStoploss,
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
        
        public void PreTradeValidate(OrderFulfillmentPlan orderFulfillmentPlan, IMatchingEngineBase matchingEngine)
        {
            GetAssetPairIfAvailableForTrading(orderFulfillmentPlan.Order.AssetPairId, 
                orderFulfillmentPlan.Order.OrderType, 
                orderFulfillmentPlan.RequiresPositionOpening, 
                true);

            ValidateTradeLimits(orderFulfillmentPlan);

            if (orderFulfillmentPlan.RequiresPositionOpening)
            {
                _accountUpdateService.CheckBalance(orderFulfillmentPlan, matchingEngine);
            }
        }

        public bool CheckIfPendingOrderExecutionPossible(string assetPairId, OrderType orderType, bool shouldOpenNewPosition)
        {
            try
            {
                GetAssetPairIfAvailableForTrading(assetPairId, orderType, shouldOpenNewPosition, true);
            }
            catch
            {
                return false;
            }

            return true;
        }
        
        public IAssetPair GetAssetPairIfAvailableForTrading(string assetPairId, OrderType orderType,
            bool shouldOpenNewPosition, bool isPreTradeValidation, bool validateForEdit = false)
        {
            if (isPreTradeValidation || orderType == OrderType.Market)
            {
                var tradingStatus = _assetDayOffService.IsAssetTradingDisabled(assetPairId);
                if (tradingStatus)
                {
                    if (tradingStatus.Reason == InstrumentTradingDisabledReason.InstrumentTradingDisabled)
                    {
                        throw new OrderRejectionException(OrderRejectReason.InstrumentTradingDisabled, 
                            $"Trading for the instrument {assetPairId} is disabled.");
                    }

                    throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                        $"Trades for instrument {assetPairId} are not available due to trading is closed");
                }
            }
            else if (_assetDayOffService.ArePendingOrdersDisabled(assetPairId))
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                    $"Pending orders for instrument {assetPairId} are not available");
            }

            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(assetPairId); 
            
            if (assetPair == null)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidInstrument, $"Instrument {assetPairId} not found");
            }
            
            if (assetPair.IsDiscontinued)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidInstrument, 
                    $"Trading for the instrument {assetPairId} is discontinued");
            }

            if (assetPair.IsTradingDisabled && !validateForEdit)
            {
                throw new OrderRejectionException(OrderRejectReason.InstrumentTradingDisabled, 
                    $"Trading for the instrument {assetPairId} is disabled.");
            }

            if (assetPair.IsSuspended && shouldOpenNewPosition)
            {
                if (isPreTradeValidation)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                        $"Orders execution for instrument {assetPairId} is temporarily unavailable (instrument is suspended)");
                }

                if (orderType == OrderType.Market)
                {
                    throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                        $"Market orders for instrument {assetPairId} are temporarily unavailable (instrument is suspended)");
                }
            }

            if (assetPair.IsFrozen && shouldOpenNewPosition)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                    $"Opening new positions for instrument {assetPairId} is temporarily unavailable (instrument is frozen)");
            }

            return assetPair;
        }

        private void ValidateTradeLimits(OrderFulfillmentPlan orderFulfillmentPlan)
        {
            var tradingInstrument = _tradingInstrumentsCache.GetTradingInstrument(
                    orderFulfillmentPlan.Order.TradingConditionId,
                    orderFulfillmentPlan.Order.AssetPairId);

            if (tradingInstrument == null)
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidInstrument,
                    $"The instrument is not found for the trading condition {orderFulfillmentPlan.Order.TradingConditionId} and asset pair {orderFulfillmentPlan.Order.AssetPairId}");
            }

            var unfulfilledAbsVolume = Math.Abs(orderFulfillmentPlan.UnfulfilledVolume);
            
            if (tradingInstrument.DealMaxLimit > 0 &&
                unfulfilledAbsVolume > tradingInstrument.DealMaxLimit &&
                orderFulfillmentPlan.RequiresPositionOpening)
            {
                throw new OrderRejectionException(OrderRejectReason.MaxOrderSizeLimit,
                    $"The volume of a single order is limited to {tradingInstrument.DealMaxLimit} {tradingInstrument.Instrument} but was {orderFulfillmentPlan.UnfulfilledVolume}. Order id = [{orderFulfillmentPlan.Order.Id}]");
            }

            var positionsAbsVolume = _ordersCache.Positions
                .GetPositionsByInstrumentAndAccount(orderFulfillmentPlan.Order.AssetPairId, orderFulfillmentPlan.Order.AccountId)
                .Sum(o => Math.Abs(o.Volume));

            var oppositePositionsToBeClosedAbsVolume =
                Math.Abs(orderFulfillmentPlan.Order.Volume) - unfulfilledAbsVolume;

            if (tradingInstrument.PositionLimit > 0 &&
                orderFulfillmentPlan.RequiresPositionOpening &&
                positionsAbsVolume - oppositePositionsToBeClosedAbsVolume + unfulfilledAbsVolume > tradingInstrument.PositionLimit)
            {
                throw new OrderRejectionException(OrderRejectReason.MaxPositionLimit,
                    $"The ABSOLUTE volume of open positions is limited to {tradingInstrument.PositionLimit} {tradingInstrument.Instrument}.");
            }

            if (orderFulfillmentPlan.RequiresPositionOpening &&
                orderFulfillmentPlan.Order.Direction == OrderDirection.Sell &&
                !tradingInstrument.ShortPosition)
            {
                throw new OrderRejectionException(OrderRejectReason.ShortPositionsDisabled,
                    $"Short positions are disabled for {tradingInstrument.Instrument}.");
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
