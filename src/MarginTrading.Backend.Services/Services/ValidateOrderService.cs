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
        private readonly ITradingInstrumentsCacheService _accountAssetsCacheService;
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
            _accountAssetsCacheService = accountAssetsCacheService;
            _assetPairsCache = assetPairsCache;
            _ordersCache = ordersCache;
            _assetDayOffService = assetDayOffService;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
            _marginSettings = marginSettings;
            _cfdCalculatorService = cfdCalculatorService;
        }
        
        #region Base validations
        
        public async Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndGetOrders(
            OrderPlaceRequest request)
        {
            
            #region Validate properties
            
            if (request.Volume == 0)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume, "Volume can not be 0");
            }
            
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(request.InstrumentId); 
            
            if (assetPair == null)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, "Instrument not found");
            }

            if (!request.Price.HasValue)
            {
                if (request.Type != OrderTypeContract.Market)
                    throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                        $"Price is required for {request.Type} order");
            }
            else
            {
                request.Price = Math.Round(request.Price.Value, assetPair.Accuracy);

                if (request.Price == 0)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                        $"Price can not be 0");
                }
            }
            
            var account = _accountsCacheService.TryGet(request.AccountId);

            if (account == null)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidAccount, "Account not found");
            }
            
            if (!_quoteCashService.TryGetQuoteById(request.InstrumentId, out var quote))
            {
                throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "Quote not found");
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

            #endregion

            if (request.Type == OrderTypeContract.StopLoss ||
                request.Type == OrderTypeContract.TakeProfit ||
                request.Type == OrderTypeContract.TrailingStop)
            {
                var order = await ValidateAndGetSlorTpOrder(request, request.Type, request.Price, equivalentSettings,
                    null);

                return (order, new List<Order>());
            }

            //TODO: add setting for every type of validation (needed or not)
            //ValidateLimitPrice(request, assetPair, quote);
            //ValidateOrderStops();
			
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
                request.AdditionalInfo);
            
            var relatedOrders = new List<Order>();

            if (request.StopLoss.HasValue)
            {
                request.StopLoss = Math.Round(request.StopLoss.Value, assetPair.Accuracy);

                if (request.StopLoss == 0)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                        $"StopLoss can not be 0");
                }

                var orderType = request.UseTrailingStop ? OrderTypeContract.TrailingStop : OrderTypeContract.StopLoss;
                
                var sl = await ValidateAndGetSlorTpOrder(request, orderType, request.StopLoss,
                    equivalentSettings, baseOrder);
                
                if (sl != null)
                    relatedOrders.Add(sl);    
            }
            
            if (request.TakeProfit.HasValue)
            {
                request.TakeProfit = Math.Round(request.TakeProfit.Value, assetPair.Accuracy);

                if (request.TakeProfit == 0)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                        $"TakeProfit can not be 0");
                }

                var tp = await ValidateAndGetSlorTpOrder(request, OrderTypeContract.TakeProfit, request.TakeProfit,
                    equivalentSettings, baseOrder);
                
                if (tp != null)
                    relatedOrders.Add(tp);    
            }

            return (baseOrder, relatedOrders);
        }
        
        //TODO: check, if we need to validate SL and TP prices
        private async Task<Order> ValidateAndGetSlorTpOrder(OrderPlaceRequest request, OrderTypeContract type,
            decimal? price, ReportingEquivalentPricesSettings equivalentSettings, Order parentOrder)
        {
            if (parentOrder == null)
            {
                if (!string.IsNullOrEmpty(request.ParentOrderId))
                {
                    parentOrder = _ordersCache.GetOrderById(request.ParentOrderId);
                }
            }

            if (parentOrder != null)
            {
                var initialParameters = await GetOrderInitialParameters(parentOrder.AssetPairId,
                    parentOrder.LegalEntity, equivalentSettings, parentOrder.AccountAssetId);

                var originator = GetOriginator(request.Originator);
                
                return new Order(initialParameters.id, initialParameters.code, parentOrder.AssetPairId,
                    -parentOrder.Volume, initialParameters.now, initialParameters.now,
                    request.Validity, parentOrder.AccountId, parentOrder.TradingConditionId, parentOrder.AccountAssetId,
                    price, parentOrder.EquivalentAsset, OrderFillType.FillOrKill, string.Empty,
                    parentOrder.LegalEntity, false, type.ToType<OrderType>(), parentOrder.Id, null,
                    originator, initialParameters.equivalentPrice,
                    initialParameters.fxPrice, OrderStatus.Placed, request.AdditionalInfo);
            }

            if (!string.IsNullOrEmpty(request.PositionId))
            {
                var position = _ordersCache.Positions.GetOrderById(request.PositionId);

                var initialParameters = await GetOrderInitialParameters(position.AssetPairId,
                    position.LegalEntity, equivalentSettings, position.AccountAssetId);
                
                var originator = GetOriginator(request.Originator);

                return new Order(initialParameters.id, initialParameters.code, position.AssetPairId,
                    -position.Volume, initialParameters.now, initialParameters.now,
                    request.Validity, position.AccountId, position.TradingConditionId, position.AccountAssetId,
                    price, position.EquivalentAsset, OrderFillType.FillOrKill, string.Empty,
                    position.LegalEntity, false, type.ToType<OrderType>(), null, position.Id,
                    originator, initialParameters.equivalentPrice,
                    initialParameters.fxPrice, OrderStatus.Placed, request.AdditionalInfo);
            }

            throw new ValidateOrderException(OrderRejectReason.InvalidParent,
                "Related order must have parent order or position");
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
        
        private void ValidateLimitPrice(OrderPlaceRequest request, IAssetPair assetPair, InstrumentBidAskPair quote)
        {
            if (request.Type != OrderTypeContract.Limit && request.Type != OrderTypeContract.Stop)
                return;

            if (_assetDayOffService.ArePendingOrdersDisabled(request.InstrumentId))
            {
                throw new ValidateOrderException(OrderRejectReason.NoLiquidity,
                    "Trades for instrument are not available");
            }

            if (request.Price <= 0)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice,
                    "Incorrect expected open price");
            }

            request.Price = Math.Round(request.Price ?? 0, assetPair.Accuracy);

            if (request.Direction == OrderDirectionContract.Buy && request.Price > quote.Ask ||
                request.Direction == OrderDirectionContract.Sell && request.Price < quote.Bid)
            {
                var reasonText = request.Direction == OrderDirectionContract.Buy
                    ? string.Format(MtMessages.Validation_PriceAboveAsk, request.Price, quote.Ask)
                    : string.Format(MtMessages.Validation_PriceBelowBid, request.Price, quote.Bid);

                throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice, reasonText,
                    $"{request.InstrumentId} quote (bid/ask): {quote.Bid}/{quote.Ask}");
            }
        }
        
        public void ValidateOrderStops(OrderDirection type, BidAskPair quote, decimal deltaBid, decimal deltaAsk, decimal? takeProfit,
            decimal? stopLoss, decimal? expectedOpenPrice, int assetAccuracy)
        {
            var deltaBidValue = MarginTradingCalculations.GetVolumeFromPoints(deltaBid, assetAccuracy);
            var deltaAskValue = MarginTradingCalculations.GetVolumeFromPoints(deltaAsk, assetAccuracy);

            if (expectedOpenPrice.HasValue)
            {
                decimal minGray;
                decimal maxGray;

                //check TP/SL for pending order
                if (type == OrderDirection.Buy)
                {
                    minGray = Math.Round(expectedOpenPrice.Value - 2 * deltaBidValue, assetAccuracy);
                    maxGray = Math.Round(expectedOpenPrice.Value + deltaAskValue, assetAccuracy);

                    if (takeProfit.HasValue && takeProfit > 0 && takeProfit < maxGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                            string.Format(MtMessages.Validation_TakeProfitMustBeMore, Math.Round((decimal) takeProfit, assetAccuracy), maxGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }

                    if (stopLoss.HasValue && stopLoss > 0 && stopLoss > minGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                            string.Format(MtMessages.Validation_StopLossMustBeLess, Math.Round((decimal) stopLoss, assetAccuracy), minGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }
                }
                else
                {
                    minGray = Math.Round(expectedOpenPrice.Value - deltaBidValue, assetAccuracy);
                    maxGray = Math.Round(expectedOpenPrice.Value + 2 * deltaAskValue, assetAccuracy);

                    if (takeProfit.HasValue && takeProfit > 0 && takeProfit > minGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                            string.Format(MtMessages.Validation_TakeProfitMustBeLess, Math.Round((decimal) takeProfit, assetAccuracy), minGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }

                    if (stopLoss.HasValue && stopLoss > 0 && stopLoss < maxGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                            string.Format(MtMessages.Validation_StopLossMustBeMore, Math.Round((decimal) stopLoss, assetAccuracy), maxGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }
                }
            }
            else
            {
                //check TP/SL for market order
                var minGray = Math.Round(quote.Bid - deltaBidValue, assetAccuracy);
                var maxGray = Math.Round(quote.Ask + deltaAskValue, assetAccuracy);

                if (type == OrderDirection.Buy)
                {
                    if (takeProfit.HasValue && takeProfit > 0 && takeProfit < maxGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                            string.Format(MtMessages.Validation_TakeProfitMustBeMore, Math.Round((decimal) takeProfit, assetAccuracy), maxGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }

                    if (stopLoss.HasValue && stopLoss > 0 && stopLoss > minGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                            string.Format(MtMessages.Validation_StopLossMustBeLess, Math.Round((decimal) stopLoss, assetAccuracy), minGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }
                }
                else
                {
                    if (takeProfit.HasValue && takeProfit > 0 && takeProfit > minGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidTakeProfit,
                            string.Format(MtMessages.Validation_TakeProfitMustBeLess, Math.Round((decimal) takeProfit, assetAccuracy), minGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }

                    if (stopLoss.HasValue && stopLoss > 0 && stopLoss < maxGray)
                    {
                        throw new ValidateOrderException(OrderRejectReason.InvalidStoploss,
                            string.Format(MtMessages.Validation_StopLossMustBeMore,
                                Math.Round((decimal) stopLoss, assetAccuracy), maxGray),
                            $"quote (bid/ask): {quote.Bid}/{quote.Ask}");
                    }
                }
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
        
        public void MakePreTradeValidation(Order order, bool validateMargin)
        {
            ValidateAssetPairIsAvailableForTrading(order.AssetPairId, order.TradingConditionId);

            ValidateTradeLimits(order.AssetPairId, order.TradingConditionId, order.AccountId, order.Volume);

            if (validateMargin)
                ValidateMargin(order);

        }
        
        //TODO: validate instrument status + schedule settings 
        private void ValidateAssetPairIsAvailableForTrading(string assetPairId, string tradingConditionId)
        {
//            if (_assetDayOffService.IsDayOff(request.InstrumentId))
//            {
//                throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "Trades for instrument are not available");
//            }
        }

        private void ValidateTradeLimits(string assetPairId, string tradingConditionId, string accountId, decimal volume)
        {
            var tradingInstrument =
                _accountAssetsCacheService.GetTradingInstrument(tradingConditionId, assetPairId);

            if (tradingInstrument.DealMaxLimit > 0 && Math.Abs(volume) > tradingInstrument.DealMaxLimit)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume,
                    $"Margin Trading is in beta testing. The volume of a single order is temporarily limited to {tradingInstrument.DealMaxLimit} {tradingInstrument.Instrument}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
            }

            var existingPositionsVolume = _ordersCache.Positions.GetOrdersByInstrumentAndAccount(assetPairId, accountId)
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
        
        #endregion


        

        
    }
}
