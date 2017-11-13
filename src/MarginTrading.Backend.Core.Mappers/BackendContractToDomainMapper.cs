﻿using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Contract.BackendContracts;

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
                OpenCrossPrice = src.OpenCrossPrice,
                CloseCrossPrice = src.CloseCrossPrice
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
                OrderId = src.OrderId
            };
        }
    }
}