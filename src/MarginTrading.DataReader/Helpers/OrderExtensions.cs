using System.Linq;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.DataReader.Helpers
{
    internal static class OrderExtensions
    {
        public static OrderContract ToBaseContract(this Order src)
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

            return new OrderContract
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
                Fpl = src.FplData.Fpl,
                PnL = src.FplData.TotalFplSnapshot,
                CloseReason = src.CloseReason,
                RejectReason = src.RejectReason,
                RejectReasonText = src.RejectReasonText,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                MatchedOrders = src.MatchedOrders.Select(MatchedOrderToBackendContract).ToList(),
                MatchedCloseOrders = src.MatchedCloseOrders.Select(MatchedOrderToBackendContract).ToList()
            };
        }

        public static OrderHistoryBackendContract ToBackendHistoryContract(this Order src)
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
                TotalPnl = src.FplData.TotalFplSnapshot,
                Pnl = src.FplData.Fpl,
                InterestRateSwap = src.FplData.SwapsSnapshot,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission()
            };
        }

    }
}
