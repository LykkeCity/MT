using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.BackendContracts.AccountsManagement;
using MarginTrading.Contract.BackendContracts.TradingConditions;

namespace MarginTrading.Backend.Core.Mappers
{
    public static class DomainToBackendContractMapper
    {
        public static MarginTradingAccountBackendContract ToFullBackendContract(this IMarginTradingAccount src, bool isLive)
        {
            return new MarginTradingAccountBackendContract
            {
                Id = src.Id,
                ClientId = src.ClientId,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                MarginCall = src.GetMarginCallLevel(),
                StopOut = src.GetStopOutLevel(),
                TotalCapital = src.GetTotalCapital(),
                FreeMargin = src.GetFreeMargin(),
                MarginAvailable = src.GetMarginAvailable(),
                UsedMargin = src.GetUsedMargin(),
                MarginInit = src.GetMarginInit(),
                PnL = src.GetPnl(),
                OpenPositionsCount = src.GetOpenPositionsCount(),
                MarginUsageLevel = src.GetMarginUsageLevel(),
                IsLive = isLive,
                LegalEntity = src.LegalEntity,
            };
        }

        public static AssetPairBackendContract ToBackendContract(this IAssetPair src)
        {
            return new AssetPairBackendContract
            {
                Id = src.Id,
                Name = src.Name,
                BaseAssetId = src.BaseAssetId,
                QuoteAssetId = src.QuoteAssetId,
                Accuracy = src.Accuracy
            };
        }

        public static AccountAssetPairModel ToBackendContract(this IAccountAssetPair src)
        {
            return new AccountAssetPairModel
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Instrument = src.Instrument,
                LeverageInit = src.LeverageInit,
                LeverageMaintenance = src.LeverageMaintenance,
                SwapLong = src.SwapLong,
                SwapShort = src.SwapShort,
                OvernightSwapLong = src.OvernightSwapLong,
                OvernightSwapShort =src.OvernightSwapShort,
                CommissionLong = src.CommissionLong,
                CommissionShort = src.CommissionShort,
                CommissionLot = src.CommissionLot,
                DeltaBid = src.DeltaBid,
                DeltaAsk = src.DeltaAsk,
                DealLimit = src.DealLimit,
                PositionLimit = src.PositionLimit
            };
        }

        public static GraphBidAskPairBackendContract ToBackendContract(this GraphBidAskPair src)
        {
            return new GraphBidAskPairBackendContract
            {
                Ask = src.Ask,
                Bid = src.Bid,
                Date = src.Date
            };
        }

        public static AggregatedOrderBookItemBackendContract ToBackendContract(this OrderBookLevel src)
        {
            return new AggregatedOrderBookItemBackendContract
            {
                Price = src.Price,
                Volume = src.Volume
            };
        }

        public static AccountHistoryBackendContract ToBackendContract(this IMarginTradingAccountHistory src)
        {
            return new AccountHistoryBackendContract
            {
                Id = src.Id,
                Date = src.Date,
                AccountId = src.AccountId,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type.ToType<AccountHistoryTypeContract>(),
                OrderId = src.OrderId,
                LegalEntity = src.LegalEntity,
                AuditLog = src.AuditLog
            };
        }

        public static OrderHistoryBackendContract ToBackendHistoryContract(this IOrder src)
        {
            return new OrderHistoryBackendContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                AssetAccuracy = src.AssetAccuracy,
                Type = src.GetOrderType().ToType<OrderDirectionContract>(),
                Status = src.Status.ToType<OrderStatusContract>(),
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                TotalPnl = src.GetTotalFpl(),
                Pnl = src.GetFpl(),
                InterestRateSwap = src.GetSwaps(),
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };
        }

        public static OrderHistoryBackendContract ToBackendHistoryContract(this IOrderHistory src)
        {
            return new OrderHistoryBackendContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                AssetAccuracy = src.AssetAccuracy,
                Type = src.Type.ToType<OrderDirectionContract>(),
                Status = src.Status.ToType<OrderStatusContract>(),
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Pnl = src.Fpl,
                TotalPnl = src.PnL,
                InterestRateSwap = src.InterestRateSwap,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };
        }

        public static OrderHistoryBackendContract ToBackendHistoryOpenedContract(this IOrderHistory src)
        {
            return new OrderHistoryBackendContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                AssetAccuracy = src.AssetAccuracy,
                Type = src.Type.ToType<OrderDirectionContract>(),
                Status = OrderStatus.Active.ToType<OrderStatusContract>(),
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                OpenDate = src.OpenDate,
                CloseDate = null,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                TotalPnl = src.PnL,
                Pnl = src.Fpl,
                InterestRateSwap = src.InterestRateSwap,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };
        }

        public static OrderBackendContract ToBackendContract(this IOrder src)
        {
            return new OrderBackendContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                Type = src.GetOrderType().ToType<OrderDirectionContract>(),
                Status = src.Status.ToType<OrderStatusContract>(),
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                RejectReason = src.RejectReason.ToType<OrderRejectReasonContract>(),
                RejectReasonText = src.RejectReasonText,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                MatchedVolume = src.GetMatchedVolume(),
                MatchedCloseVolume = src.GetMatchedCloseVolume(),
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.GetTotalFpl(),
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };
        }

        public static OrderFullContract ToFullContract(this IOrder src, OrderUpdateType orderUpdateType)
        {
            var orderContract = new OrderFullContract
            {
                Id = src.Id,
                ClientId = src.ClientId,
                AccountId = src.AccountId,
                TradingConditionId = src.TradingConditionId,
                AccountAssetId = src.AccountAssetId,
                Instrument = src.Instrument,
                Type = src.GetOrderType().ToType<OrderDirectionContract>(),
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                QuoteRate = src.GetQuoteRate(),
                MarginRate = src.GetMarginRate(),
                AssetAccuracy = src.AssetAccuracy,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                StartClosingDate = src.StartClosingDate,
                Status = src.Status.ToType<OrderStatusContract>(),
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                FillType = src.FillType.ToType<OrderFillTypeContract>(),
                RejectReason = src.RejectReason.ToType<OrderRejectReasonContract>(),
                RejectReasonText = src.RejectReasonText,
                Comment = src.Comment,
                MatchedVolume = src.GetMatchedVolume(),
                MatchedCloseVolume = src.GetMatchedCloseVolume(),
                PnL = src.GetTotalFpl(),
                Fpl = src.GetFpl(),
                InterestRateSwap = src.GetSwaps(),
                MarginInit = src.GetMarginInit(),
                MarginMaintenance = src.GetMarginMaintenance(),
                OrderUpdateType = orderUpdateType.ToType<OrderUpdateTypeContract>(),
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };

            foreach (var order in src.MatchedOrders)
            {
                orderContract.MatchedOrders.Add(order.ToBackendContract());
            }

            foreach (var order in src.MatchedCloseOrders)
            {
                orderContract.MatchedCloseOrders.Add(order.ToBackendContract());
            }

            return orderContract;
        }

        public static OrderContract ToBaseContract(this IOrder src)
        {
            var orderContract = new OrderContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                AccountAssetId = src.AccountAssetId,
                ClientId = src.ClientId,
                Instrument = src.Instrument,
                Status = src.Status.ToType<OrderStatusContract>(),
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Type = src.GetOrderType().ToType<OrderDirectionContract>(),
                Volume = src.Volume,
                MatchedVolume = src.GetMatchedVolume(),
                MatchedCloseVolume = src.GetMatchedCloseVolume(),
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.GetFpl(),
                PnL = src.GetTotalFpl(),
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                RejectReason = src.RejectReason.ToType<OrderRejectReasonContract>(),
                RejectReasonText = src.RejectReasonText,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };

            foreach (var order in src.MatchedOrders)
            {
                orderContract.MatchedOrders.Add(order.ToBackendContract());
            }

            foreach (var order in src.MatchedCloseOrders)
            {
                orderContract.MatchedCloseOrders.Add(order.ToBackendContract());
            }

            return orderContract;
        }


        public static MatchedOrderBackendContract ToBackendContract(this MatchedOrder src)
        {
            return new MatchedOrderBackendContract
            {
                OrderId = src.OrderId,
                MarketMakerId = src.MarketMakerId,
                LimitOrderLeftToMatch = src.LimitOrderLeftToMatch,
                Volume = src.Volume,
                Price = src.Price,
                MatchedDate = src.MatchedDate
            };
        }

        public static LimitOrderBackendContract ToBackendContract(this LimitOrder src)
        {
            return new LimitOrderBackendContract
            {
                Id = src.Id,
                MarketMakerId = src.MarketMakerId,
                Instrument = src.Instrument,
                Volume = src.Volume,
                Price = src.Price,
                CreateDate = src.CreateDate,
                MatchedOrders = src.MatchedOrders.Select(item => item.ToBackendContract()).ToArray()
            };
        }

        public static OrderBookBackendContract ToBackendContract(this OrderBook orderbook)
        {
            return new OrderBookBackendContract
            {
                Buy = orderbook.Buy.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray()),
                Sell = orderbook.Sell.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray()),
            };
        }

        public static InstrumentBidAskPairContract ToBackendContract(this InstrumentBidAskPair src)
        {
            return new InstrumentBidAskPairContract
            {
                Id = src.Instrument,
                Date = src.Date,
                Bid = src.Bid,
                Ask = src.Ask
            };
        }

        public static TradingConditionModel ToBackendContract(this ITradingCondition src)
        {
            return new TradingConditionModel
            {
                Id = src.Id,
                Name = src.Name,
                IsDefault = src.IsDefault,
                LegalEntity = src.LegalEntity,
            };
        }
        
        public static AccountGroupModel ToBackendContract(this IAccountGroup src)
        {
            return new AccountGroupModel
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                MarginCall = src.MarginCall,
                StopOut = src.StopOut,
                DepositTransferLimit = src.DepositTransferLimit,
                ProfitWithdrawalLimit = src.ProfitWithdrawalLimit
            };
        }

        public static MarginTradingAccountModel ToBackendContract(this IMarginTradingAccount src)
        {
            return new MarginTradingAccountModel
            {
                Id = src.Id,
                ClientId = src.ClientId,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                LegalEntity = src.LegalEntity,
            };
        }
    }
}
