using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.DataReader.Helpers
{
    public static class OrderHistoryExtensions
    {
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
                ClosePriceEquivalent = src.ClosePriceEquivalent
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
