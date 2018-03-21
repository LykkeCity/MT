using MarginTrading.Backend.Contracts.AccountHistory;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;

namespace MarginTrading.DataReader.Helpers
{
    public static class OrderHistoryExtensions
    {
        public static OrderHistoryContract ToBackendHistoryContract(this IOrderHistory src)
        {
            return new OrderHistoryContract
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
                ClosePriceEquivalent = src.ClosePriceEquivalent
            };
        }

        public static OrderHistoryContract ToBackendHistoryOpenedContract(this IOrderHistory src)
        {
            return new OrderHistoryContract
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                AssetAccuracy = src.AssetAccuracy,
                Type = src.Type.ToType<OrderDirectionContract>(),
                Status = OrderStatusContract.Active,
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
                ClosePriceEquivalent = src.ClosePriceEquivalent
            };
        }
    }
}
