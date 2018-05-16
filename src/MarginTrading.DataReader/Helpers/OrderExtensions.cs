using System.Linq;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Common.Extensions;
using MarginTrading.Backend.Contracts.TradeMonitoring;

namespace MarginTrading.DataReader.Helpers
{
    internal static class OrderExtensions
    {
        public static DetailedOrderContract ToBaseContract(this Order src)
        {
            MatchedOrderBackendContract MatchedOrderToBackendContract(MatchedOrder o)
                => new MatchedOrderBackendContract
                {
                    OrderId = o.OrderId,
                    LimitOrderLeftToMatch = o.LimitOrderLeftToMatch,
                    Volume = o.Volume,
                    Price = o.Price,
                    MatchedDate = o.MatchedDate
                };

            return new DetailedOrderContract
            {
                Id = src.Id,
                Code = src.Code,
                AccountId = src.AccountId,
                AccountAssetId = src.AccountAssetId,
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
                Fpl = src.FplData.Fpl,
                PnL = src.FplData.TotalFplSnapshot,
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                RejectReason = src.RejectReason.ToType<OrderRejectReasonContract>(),
                RejectReasonText = src.RejectReasonText,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                MatchedOrders = src.MatchedOrders.Select(MatchedOrderToBackendContract).ToList(),
                MatchedCloseOrders = src.MatchedCloseOrders.Select(MatchedOrderToBackendContract).ToList(),
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };
        }
    }
}
