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

        public static AccountHistoryBackendRequest ToBackendContract(this AccountHistoryFiltersClientRequest src, string clientId)
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

        public static OpenOrderBackendRequest ToBackendContract(this NewOrderClientContract src, string clientId)
        {
            return new OpenOrderBackendRequest
            {
                ClientId = clientId,
                Order = src.ToBackendContract()
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
    }
}
