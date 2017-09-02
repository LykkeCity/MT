using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;

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
    }
}
