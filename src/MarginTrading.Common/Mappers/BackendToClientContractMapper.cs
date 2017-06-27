using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;

namespace MarginTrading.Common.Mappers
{
    public static class BackendToClientContractMapper
    {
        public static MarginTradingAccountClientContract ToClientContract(
            this MarginTradingAccountBackendContract src)
        {
            return new MarginTradingAccountClientContract
            {
                Id = src.Id,
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                IsCurrent = src.IsCurrent,
                MarginCall = src.MarginCall,
                StopOut = src.StopOut,
                TotalCapital = src.TotalCapital,
                FreeMargin = src.FreeMargin,
                MarginAvailable = src.MarginAvailable,
                UsedMargin = src.UsedMargin,
                MarginInit = src.MarginInit,
                PnL = src.PnL,
                OpenPositionsCount = src.OpenPositionsCount,
                MarginUsageLevel = src.MarginUsageLevel,
                IsLive = src.IsLive
            };
        }

        public static MarginTradingAssetClientContract ToClientContract(
            this MarginTradingAssetBackendContract src)
        {
            return new MarginTradingAssetClientContract
            {
                Id = src.Id,
                Name = src.Name,
                BaseAssetId = src.BaseAssetId,
                QuoteAssetId = src.QuoteAssetId,
                Accuracy = src.Accuracy
            };
        }

        public static MarginTradingAccountAssetClientContract ToClientContract(
            this MarginTradingAccountAssetBackendContract src)
        {
            return new MarginTradingAccountAssetClientContract
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Instrument = src.Instrument,
                LeverageInit = src.LeverageInit,
                LeverageMaintenance = src.LeverageMaintenance,
                SwapLong = src.SwapLong,
                SwapShort = src.SwapShort,
                SwapLongPct = src.SwapLongPct,
                SwapShortPct = src.SwapShortPct,
                CommissionLong = src.CommissionLong,
                CommissionShort = src.CommissionShort,
                CommissionLot = src.CommissionLot,
                DeltaBid = src.DeltaBid,
                DeltaAsk = src.DeltaAsk,
                DealLimit = src.DealLimit,
                PositionLimit = src.PositionLimit
            };
        }

        public static GraphBidAskPairClientContract ToClientContract(
            this GraphBidAskPairBackendContract src)
        {
            return new GraphBidAskPairClientContract
            {
                Bid = src.Bid,
                Ask = src.Ask,
                Date = src.Date
            };
        }

        public static InitDataClientResponse ToClientContract(this InitDataBackendResponse src)
        {
            return new InitDataClientResponse
            {
                Accounts = src.Accounts.Select(item => item.ToClientContract()).ToArray(),
                TradingConditions = src.AccountAssetPairs.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToClientContract()).ToArray())
            };
        }

        public static InitChartDataClientResponse ToClientContract(this InitChartDataBackendResponse src)
        {
            return new InitChartDataClientResponse
            {
                ChartData = src.ChartData.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToClientContract()).ToArray())
            };
        }

        public static InitAccountInstrumentsClientResponse ToClientContract(this InitAccountInstrumentsBackendResponse src)
        {
            return new InitAccountInstrumentsClientResponse
            {
                TradingConditions = src.AccountAssets.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToClientContract()).ToArray())
            };
        }

        public static AggregatedOrderBookItemClientContract ToClientContract(
            this AggregatedOrderBookItemBackendContract src)
        {
            return new AggregatedOrderBookItemClientContract
            {
                Price = src.Price,
                Volume = src.Volume
            };
        }

        public static AggregatedOrderbookClientContract ToClientContract(this AggregatedOrderbookBackendResponse src)
        {
            return new AggregatedOrderbookClientContract
            {
                Buy = src.Buy.Select(item => item.ToClientContract()).ToArray(),
                Sell = src.Sell.Select(item => item.ToClientContract()).ToArray(),
            };
        }

        public static MtClientResponse<bool> ToClientContract(this MtBackendResponse<bool> src)
        {
            return new MtClientResponse<bool>
            {
                Result = src.Result,
                Message = src.Message
            };
        }

        public static AccountHistoryClientContract ToClientContract(this AccountHistoryBackendContract src)
        {
            return new AccountHistoryClientContract
            {
                Id = src.Id,
                Date = src.Date,
                AccountId = src.AccountId,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                Comment = src.Comment,
                Type = src.Type
            };
        }

        public static OrderHistoryClientContract ToClientContract(this OrderHistoryBackendContract src)
        {
            return new OrderHistoryClientContract
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
                Fpl = src.TotalPnl,
                TotalPnL = src.TotalPnl,
                PnL = src.Pnl,
                InterestRateSwap = src.InterestRateSwap,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission
            };
        }

        public static AccountHistoryClientResponse ToClientContract(this AccountHistoryBackendResponse src)
        {
            return new AccountHistoryClientResponse
            {
                Account = src.Account.Select(item => item.ToClientContract()).OrderByDescending(item => item.Date).ToArray(),
                OpenPositions = src.OpenPositions.Select(item => item.ToClientContract()).ToArray(),
                PositionsHistory = src.PositionsHistory.Select(item => item.ToClientContract()).ToArray()
            };
        }

        public static NewOrderClientContract ToClientContract(this NewOrderBackendContract src)
        {
            return new NewOrderClientContract
            {
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                FillType = src.FillType
            };
        }

        public static OrderClientContract ToClientContract(this OrderBackendContract src)
        {
            return new OrderClientContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                Type = (int)src.Type,
                Status = (int)src.Status,
                CloseReason = (int)src.CloseReason,
                RejectReason = (int)src.RejectReason,
                RejectReasonText = src.RejectReasonText,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                Volume = src.Volume,
                MatchedVolume = src.MatchedVolume,
                MatchedCloseVolume = src.MatchedCloseVolume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.Fpl,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                SwapCommission = src.SwapCommission
            };
        }

        public static OrderClientContract ToClientContract(this OrderContract src)
        {
            return new OrderClientContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                Type = (int)src.Type,
                Status = (int)src.Status,
                CloseReason = (int)src.CloseReason,
                RejectReason = (int)src.RejectReason,
                RejectReasonText = src.RejectReasonText,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                Volume = src.Volume,
                MatchedVolume = src.MatchedVolume,
                MatchedCloseVolume = src.MatchedCloseVolume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.Fpl,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                SwapCommission = src.SwapCommission
            };
        }

        public static MtClientResponse<OrderClientContract> ToClientContract(this OpenOrderBackendResponse src)
        {
            return new MtClientResponse<OrderClientContract>
            {
                Result = src.Order.ToClientContract()
            };
        }

        public static MatchedOrderClientContract ToClientContract(this MatchedOrderBackendContract src)
        {
            return new MatchedOrderClientContract
            {
                OrderId = src.OrderId,
                LimitOrderLeftToMatch = src.LimitOrderLeftToMatch,
                Volume = src.Volume,
                Price = src.Price,
                MatchedDate = src.MatchedDate
            };
        }

        public static LimitOrderClientContract ToClientContract(this LimitOrderBackendContract src)
        {
            return new LimitOrderClientContract
            {
                Id = src.Id,
                MarketMakerId = src.MarketMakerId,
                Instrument = src.Instrument,
                Volume = src.Volume,
                Price = src.Price,
                CreateDate = src.CreateDate,
                MatchedOrders = src.MatchedOrders.Select(item => item.ToClientContract()).ToArray()
            };
        }

        public static OrderBookClientContract ToClientContract(this OrderBookBackendContract src)
        {
            return new OrderBookClientContract
            {
                Buy = src.Buy.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToClientContract()).ToArray()),
                Sell = src.Sell.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToClientContract()).ToArray())
            };
        }

        public static Dictionary<string, OrderBookClientContract> ToClientContract(this OrderbooksBackendResponse src)
        {
            return src.Orderbooks.ToDictionary(pair => pair.Key, pair => pair.Value.ToClientContract());
        }

        public static AccountStopoutClientContract ToClientContract(this AccountStopoutBackendContract src)
        {
            return new AccountStopoutClientContract
            {
                AccountId = src.AccountId,
                PositionsCount = src.PositionsCount,
                TotalPnl = src.TotalPnl
            };
        }

        public static UserUpdateEntityClientContract ToClientContract(this UserUpdateEntityBackendContract src)
        {
            return new UserUpdateEntityClientContract
            {
                UpdateAccountAssetPairs = src.UpdateAccountAssetPairs,
                UpdateAccounts = src.UpdateAccounts
            };
        }

        public static ClientOrdersClientResponse ToClientContract(this ClientOrdersBackendResponse src)
        {
            return new ClientOrdersClientResponse
            {
                Orders = src.Orders.Select(item => item.ToClientContract()).ToArray(),
                Positions = src.Positions.Select(item => item.ToClientContract()).ToArray()
            };
        }

        public static AccountHistoryItemClient ToClientContract(this AccountHistoryItemBackend src)
        {
            return new AccountHistoryItemClient
            {
                Date = src.Date,
                Account = src.Account?.ToClientContract(),
                Position = src.Position?.ToClientContract()
            };
        }

        public static AccountHistoryItemClient[] ToClientContract(this AccountNewHistoryBackendResponse src)
        {
            return src.HistoryItems.Select(item => item.ToClientContract()).ToArray();
        }
    }
}
