using System;
using System.Linq;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;

// ReSharper disable PossibleInvalidOperationException

namespace MarginTrading.Common.Mappers
{
    public static class ClientToBackendContractMapper
    {
        public static SetActiveAccountBackendRequest ToBackendContract(this SetActiveAccountClientRequest src, string clientId)
        {
            return new SetActiveAccountBackendRequest
            {
                AccountId = src.AccountId,
                ClientId = clientId
            };
        }

        public static AccountClientIdBackendRequest ToBackendContract(this AccountTokenClientRequest src, string clientId)
        {
            return new AccountClientIdBackendRequest
            {
                AccountId = src.AccountId,
                ClientId = clientId
            };
        }

        public static AccountHistoryBackendRequest ToBackendContract(this AccountHistoryClientRequest src, string clientId)
        {
            return new AccountHistoryBackendRequest
            {
                AccountId = src.AccountId,
                ClientId = clientId,
                From = src.From,
                To = src.To
            };
        }

        public static NewOrderBackendContract ToBackendContract(this NewOrderClientContract src)
        {
            return new NewOrderBackendContract
            {
                AccountId = src.AccountId,
                Instrument = src.Instrument,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                Volume = src.Volume.Value,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                FillType = src.FillType
            };
        }

        public static OpenOrderBackendRequest ToBackendContract(this OpenOrderClientRequest src, string clientId)
        {
            return new OpenOrderBackendRequest
            {
                ClientId = clientId,
                Order = src.Order.ToBackendContract()
            };
        }

        public static CloseOrderBackendRequest ToBackendContract(this CloseOrderClientRequest src, string clientId)
        {
            return new CloseOrderBackendRequest
            {
                ClientId = clientId,
                OrderId = src.OrderId,
                AccountId = src.AccountId
            };
        }

        public static ChangeOrderLimitsBackendRequest ToBackendContract(this ChangeOrderLimitsClientRequest src, string clientId)
        {
            return new ChangeOrderLimitsBackendRequest
            {
                ClientId = clientId,
                AccountId = src.AccountId,
                OrderId = src.OrderId,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss
            };
        }

        public static MatchedOrderBackendContract ToBackendContract(this MatchedOrderClientContract src)
        {
            return new MatchedOrderBackendContract
            {
                OrderId = src.OrderId,
                LimitOrderLeftToMatch = src.LimitOrderLeftToMatch,
                Volume = src.Volume,
                Price = src.Price,
                MatchedDate = src.MatchedDate
            };
        }

        public static LimitOrderBackendContract ToBackendContract(this LimitOrderClientContract src)
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

        public static AddLimitOrdersBackendRequest ToBackendContract(this AddLimitOrdersClientRequest src, string clientId)
        {
            return new AddLimitOrdersBackendRequest
            {
                ClientId = clientId,
                MarketMakerId = src.MarketMakerId,
                DeleteAllBuy = src.DeleteAllBuy,
                DeleteAllSell = src.DeleteAllSell,
                DeleteByInstrumentsBuy = src.DeleteByInstrumentsBuy,
                DeleteByInstrumentsSell = src.DeleteByInstrumentsSell,
                OrdersToAdd = src.OrdersToAdd.Select(item => item.ToBackendContract()).ToArray()
            };
        }
    }
}
