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

        //TODO: make all validations
        public async Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndGetOrders(
            OrderPlaceRequest request)
        {
//            if (_assetDayOffService.IsDayOff(request.InstrumentId))
//            {
//                throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "Trades for instrument are not available");
//            }

            if (request.Volume == 0)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume, "Volume cannot be 0");
            }

            var asset = _assetPairsCache.GetAssetPairByIdOrDefault(request.InstrumentId); 
            
            if (asset == null)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidInstrument, "Instrument not found");
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

            if (request.Type == OrderTypeContract.StopLoss || request.Type == OrderTypeContract.TakeProfit)
            {
                var order = await ValidateAndGetSlorTpOrder(request, account, equivalentSettings, null);

                return (order, new List<Order>());
            }
			
            //check ExpectedOpenPrice for pending order
            if (request.Price.HasValue && request.Type == OrderTypeContract.Limit)
            {
                if (_assetDayOffService.ArePendingOrdersDisabled(request.InstrumentId))
                {
                    throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "Trades for instrument are not available");
                }
                
                if (request.Price <= 0)
                {
                    throw new ValidateOrderException(OrderRejectReason.InvalidExpectedOpenPrice, "Incorrect expected open price");
                }

                request.Price = Math.Round(request.Price ?? 0, asset.Accuracy);

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
            
            var id = Guid.NewGuid().ToString("N");
            var code = await _identityGenerator.GenerateIdAsync(nameof(Position));
            var now = _dateService.Now();
            var equivalentPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(equivalentSettings.EquivalentAsset,
                request.InstrumentId, account.LegalEntity);
            var fxPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(account.BaseAssetId,
                request.InstrumentId, account.LegalEntity);
            var volume = request.Direction == OrderDirectionContract.Buy ? request.Volume : -request.Volume;

            var baseOrder =  new Order(id, code, request.InstrumentId, volume, request.Direction.ToType<OrderDirection>(), now,
                now, request.Validity, account.Id,
                account.TradingConditionId, account.BaseAssetId, request.Price,
                equivalentSettings.EquivalentAsset, OrderFillType.FillOrKill, string.Empty, account.LegalEntity,
                request.ForceOpen, request.Type.ToType<OrderType>(), request.ParentOrderId, request.PositionId,
                request.Originator.ToType<OriginatorType>(), equivalentPrice, fxPrice);
            
            var relatedOrders = new List<Order>();

            if (request.StopLoss.HasValue)
            {
                var sl = await ValidateAndGetSlorTpOrder(request, account, equivalentSettings, baseOrder);
                
                if (sl != null)
                    relatedOrders.Add(sl);    
            }
            
            if (request.TakeProfit.HasValue)
            {
                var tp = await ValidateAndGetSlorTpOrder(request, account, equivalentSettings, baseOrder);
                
                if (tp != null)
                    relatedOrders.Add(tp);    
            }

            return (baseOrder, relatedOrders);
        }

        private async Task<Order> ValidateAndGetSlorTpOrder(OrderPlaceRequest request, IMarginTradingAccount account, 
            ReportingEquivalentPricesSettings equivalentSettings, Order parentOrder)
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
                //TODO: validate SL/TP order
                
                var id = Guid.NewGuid().ToString("N");
                var code = await _identityGenerator.GenerateIdAsync(nameof(Position));
                var now = _dateService.Now();
                var equivalentPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(equivalentSettings.EquivalentAsset,
                    request.InstrumentId, account.LegalEntity);
                var fxPrice = _cfdCalculatorService.GetQuoteRateForQuoteAsset(parentOrder.AccountAssetId,
                    request.InstrumentId, account.LegalEntity);
                var direction = parentOrder.Direction == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;

                return new Order(id, code, parentOrder.AssetPairId, -parentOrder.Volume, direction, now, now,
                    request.Validity, parentOrder.AccountId, parentOrder.TradingConditionId, parentOrder.AccountAssetId,
                    request.Price, parentOrder.EquivalentAsset, OrderFillType.FillOrKill, string.Empty,
                    parentOrder.LegalEntity, false, request.Type.ToType<OrderType>(), parentOrder.Id, null,
                    request.Originator.ToType<OriginatorType>(), equivalentPrice, fxPrice);
            }
            
            //TODO: check parent position and create order

            return null;
        }
        
        //has to be beyond global lock
        public void Validate(Position order)
        {
            var accountAsset =
                _accountAssetsCacheService.GetTradingInstrument(order.TradingConditionId, order.Instrument);

            if (accountAsset.DealMaxLimit > 0 && Math.Abs(order.Volume) > accountAsset.DealMaxLimit)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume,
                    $"Margin Trading is in beta testing. The volume of a single order is temporarily limited to {accountAsset.DealMaxLimit} {accountAsset.Instrument}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
            }

            //set special account-quote instrument
//            if (_assetPairsCache.TryGetAssetPairQuoteSubst(order.AccountAssetId, order.Instrument,
//                    order.LegalEntity, out var substAssetPair))
//            {
//                order.MarginCalcInstrument = substAssetPair.Id;
//            }

            //check TP/SL
            if (order.TakeProfit.HasValue)
            {
                order.TakeProfit = Math.Round(order.TakeProfit.Value, order.AssetAccuracy);
            }

            if (order.StopLoss.HasValue)
            {
                order.StopLoss = Math.Round(order.StopLoss.Value, order.AssetAccuracy);
            }

            //ValidateOrderStops(order.GetOrderDirection(), quote, accountAsset.Delta, accountAsset.Delta, order.TakeProfit, order.StopLoss, order.ExpectedOpenPrice, order.AssetAccuracy);

            ValidateInstrumentPositionVolume(accountAsset, order);

            //if (!_accountUpdateService.IsEnoughBalance(order))
           // {
            //    throw new ValidateOrderException(OrderRejectReason.NotEnoughBalance, MtMessages.Validation_NotEnoughBalance, $"Account available balance is {account.GetTotalCapital()}");
            //}
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

        public void ValidateInstrumentPositionVolume(ITradingInstrument assetPair, Position order)
        {
            var existingPositionsVolume = _ordersCache.Positions.GetOrdersByInstrumentAndAccount(assetPair.Instrument, order.AccountId).Sum(o => o.Volume);

            if (assetPair.PositionLimit > 0 && Math.Abs(existingPositionsVolume + order.Volume) > assetPair.PositionLimit)
            {
                throw new ValidateOrderException(OrderRejectReason.InvalidVolume,
                    $"Margin Trading is in beta testing. The volume of the net open position is temporarily limited to {assetPair.PositionLimit} {assetPair.Instrument}. Thank you for using Lykke Margin Trading, the limit will be cancelled soon!");
            }
        }
    }
}
