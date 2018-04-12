using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.BackendContracts.TradingConditions;

namespace MarginTrading.Backend.Core.Mappers
{
    public static class BackendContractToDomainMapper
    {
        public static LimitOrder ToDomain(this LimitOrderBackendContract src)
        {
            return new LimitOrder
            {
                Id = src.Id,
                MarketMakerId = src.MarketMakerId,
                Instrument = src.Instrument,
                Volume = src.Volume,
                Price = src.Price,
                CreateDate = src.CreateDate,
                MatchedOrders = new MatchedOrderCollection(src.MatchedOrders.Select(ToDomain))
            };
        }

        public static MatchedOrder ToDomain(this MatchedOrderBackendContract src)
        {
            return new MatchedOrder
            {
                OrderId = src.OrderId,
                MarketMakerId = src.MarketMakerId,
                LimitOrderLeftToMatch = src.LimitOrderLeftToMatch,
                Volume = src.Volume,
                Price = src.Price,
                MatchedDate = src.MatchedDate
            };
        }

        public static IOrderHistory ToOrderHistoryDomain(this OrderFullContract src)
        {
            var orderContract = new OrderHistory
            {
                Id = src.Id,
                ClientId = src.ClientId,
                AccountId = src.AccountId,
                TradingConditionId = src.TradingConditionId,
                AccountAssetId = src.AccountAssetId,
                Instrument = src.Instrument,
                Type = src.Type.ToType<OrderDirection>(),
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                QuoteRate = src.QuoteRate,
                AssetAccuracy = src.AssetAccuracy,
                Volume = src.Volume,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.OpenCommission,
                CloseCommission = src.CloseCommission,
                SwapCommission = src.SwapCommission,
                StartClosingDate = src.StartClosingDate,
                Status = src.Status.ToType<OrderStatus>(),
                CloseReason = src.CloseReason.ToType<OrderCloseReason>(),
                FillType = src.FillType.ToType<OrderFillType>(),
                RejectReason = src.RejectReason.ToType<OrderRejectReason>(),
                RejectReasonText = src.RejectReasonText,
                Comment = src.Comment,
                MatchedVolume = src.MatchedVolume,
                MatchedCloseVolume = src.MatchedCloseVolume,
                Fpl = src.Fpl,
                PnL = src.PnL,
                InterestRateSwap = src.InterestRateSwap,
                MarginInit = src.MarginInit,
                MarginMaintenance = src.MarginMaintenance,
                EquivalentAsset = src.EquivalentAsset,
                OpenPriceEquivalent = src.OpenPriceEquivalent,
                ClosePriceEquivalent = src.ClosePriceEquivalent,
                OrderUpdateType = src.OrderUpdateType.ToType<OrderUpdateType>(),
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineMode>(),
                LegalEntity = src.LegalEntity,
            };

            foreach (var order in src.MatchedOrders)
            {
                orderContract.MatchedOrders.Add(order.ToDomain());
            }

            foreach (var order in src.MatchedCloseOrders)
            {
                orderContract.MatchedCloseOrders.Add(order.ToDomain());
            }

            return orderContract;
        }

        public static MarginTradingAccountHistory ToAccountHistoryContract(this AccountHistoryBackendContract src)
        {
            return new MarginTradingAccountHistory
            {
                Id = src.Id,
                ClientId = src.ClientId,
                AccountId = src.AccountId,
                Amount = src.Amount,
                Type = src.Type.ToType<AccountHistoryType>(),
                Date = src.Date,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                OrderId = src.OrderId,
                LegalEntity = src.LegalEntity,
                AuditLog = src.AuditLog
            };
        }

        public static ITradingCondition ToDomainContract(this TradingConditionModel src)
        {
            return new TradingCondition
            {
                Id = src.Id,
                Name = src.Name,
                IsDefault = src.IsDefault,
                LegalEntity = src.LegalEntity
            };
        }

        public static IAccountGroup ToDomainContract(this AccountGroupModel src)
        {
            return new AccountGroup
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                MarginCall = src.MarginCall,
                StopOut = src.StopOut,
                DepositTransferLimit = src.DepositTransferLimit,
                ProfitWithdrawalLimit = src.ProfitWithdrawalLimit
            };
        }

        public static IAccountAssetPair ToDomainContract(this AccountAssetPairModel src)
        {
            return new AccountAssetPair
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Instrument = src.Instrument,
                LeverageInit = src.LeverageInit,
                LeverageMaintenance = src.LeverageMaintenance,
                SwapLong = src.SwapLong,
                SwapShort = src.SwapShort,
                OvernightSwapLong = src.OvernightSwapLong,
                OvernightSwapShort = src.OvernightSwapShort,
                CommissionLong = src.CommissionLong,
                CommissionShort = src.CommissionShort,
                CommissionLot = src.CommissionLot,
                DeltaBid = src.DeltaBid,
                DeltaAsk = src.DeltaAsk,
                DealLimit = src.DealLimit,
                PositionLimit = src.PositionLimit
            };
        }
    }
}