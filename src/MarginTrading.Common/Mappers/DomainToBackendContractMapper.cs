using System.Linq;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Common.Mappers
{
    public static class DomainToBackendContractMapper
    {
        public static MarginTradingAccountBackendContract ToBackendContract(this IMarginTradingAccount src, bool isLive)
        {
            return new MarginTradingAccountBackendContract
            {
                Id = src.Id,
                ClientId = src.ClientId,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                MarginCall = src.GetMarginCall(),
                StopOut = src.GetStopOut(),
                TotalCapital = src.GetTotalCapital(),
                FreeMargin = src.GetFreeMargin(),
                MarginAvailable = src.GetMarginAvailable(),
                UsedMargin = src.GetUsedMargin(),
                MarginInit = src.GetMarginInit(),
                PnL = src.GetPnl(),
                OpenPositionsCount = src.GetOpenPositionsCount(),
                MarginUsageLevel = src.GetMarginUsageLevel(),
                IsLive = isLive
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

        public static AccountAssetPairBackendContract ToBackendContract(this IAccountAssetPair src)
        {
            return new AccountAssetPairBackendContract
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Instrument = src.Instrument,
                LeverageInit = src.LeverageInit,
                LeverageMaintenance = src.LeverageMaintenance,
                SwapLong = src.SwapLong,
                SwapShort = src.SwapShort,
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
                Type = src.Type
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
                Type = src.GetOrderType(),
                Status = src.Status,
                CloseReason = src.CloseReason,
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
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission()
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
                Type = src.Type,
                Status = src.Status,
                CloseReason = src.CloseReason,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Pnl = src.PnL,
                TotalPnl = src.Fpl,
                InterestRateSwap = src.InterestRateSwap,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission
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
                Type = src.Type,
                Status = OrderStatus.Active,
                CloseReason = src.CloseReason,
                OpenDate = src.OpenDate,
                CloseDate = null,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                TotalPnl = src.Fpl,
                Pnl = src.PnL,
                InterestRateSwap = src.InterestRateSwap,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission
            };
        }

        public static OrderBackendContract ToBackendContract(this IOrder src)
        {
            return new OrderBackendContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                Type = src.GetOrderType(),
                Status = src.Status,
                CloseReason = src.CloseReason,
                RejectReason = src.RejectReason,
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
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission
            };
        }

        public static OrderFullContract ToFullContract(this IOrder src)
        {
            var orderContract = new OrderFullContract
            {
                Id = src.Id,
                ClientId = src.ClientId,
                AccountId = src.AccountId,
                TradingConditionId = src.TradingConditionId,
                AccountAssetId = src.AccountAssetId,
                Instrument = src.Instrument,
                Type = src.GetOrderType(),
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                QuoteRate = src.GetQuoteRate(),
                AssetAccuracy = src.AssetAccuracy,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                StartClosingDate = src.StartClosingDate,
                Status = src.Status,
                CloseReason = src.CloseReason,
                FillType = src.FillType,
                RejectReason = src.RejectReason,
                RejectReasonText = src.RejectReasonText,
                Comment = src.Comment,
                MatchedVolume = src.GetMatchedVolume(),
                MatchedCloseVolume = src.GetMatchedCloseVolume(),
                PnL = src.GetTotalFpl(),
                Fpl = src.GetFpl(),
                InterestRateSwap = src.GetSwaps(),
                MarginInit = src.GetMarginInit(),
                MarginMaintenance = src.GetMarginMaintenance(),
                OpenCrossPrice = src.GetOpenCrossPrice(),
                CloseCrossPrice = src.GetCloseCrossPrice()
            };

            foreach (MatchedOrder order in src.MatchedOrders)
            {
                orderContract.MatchedOrders.Add(order.ToBackendContract());
            }

            foreach (MatchedOrder order in src.MatchedCloseOrders)
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
                Status = src.Status,
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Type = src.GetOrderType(),
                Volume = src.Volume,
                MatchedVolume = src.GetMatchedVolume(),
                MatchedCloseVolume = src.GetMatchedCloseVolume(),
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.GetFpl(),
                PnL = src.GetTotalFpl(),
                CloseReason = src.CloseReason,
                RejectReason = src.RejectReason,
                RejectReasonText = src.RejectReasonText,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission
            };

            foreach (MatchedOrder order in src.MatchedOrders)
            {
                orderContract.MatchedOrders.Add(order.ToBackendContract());
            }

            foreach (MatchedOrder order in src.MatchedCloseOrders)
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
    }
}
