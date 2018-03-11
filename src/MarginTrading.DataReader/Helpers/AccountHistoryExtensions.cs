using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Common.Extensions;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.DataReader.Helpers
{
    public static class AccountHistoryExtensions
    {
        public static AccountHistoryBackendContract ToBackendContract(this IMarginTradingAccountHistory src)
        {
            return new AccountHistoryBackendContract
            {
                Id = src.Id,
                Date = src.Date,
                AccountId = src.AccountId,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type.ToType<AccountHistoryTypeContract>(),
                OrderId = src.OrderId,
                LegalEntity = src.LegalEntity,
            };
        }
    }
}
