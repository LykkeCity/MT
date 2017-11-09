using MarginTrading.AccountReportsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities
{
    public class AccountsStatReportEntity : TableEntity
    {
        public string BaseAssetId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string AccountId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public double Balance { get; set; }
        public double WithdrawTransferLimit { get; set; }
        public double MarginCall { get; set; }
        public double StopOut { get; set; }
        public double TotalCapital { get; set; }
        public double FreeMargin { get; set; }
        public double MarginAvailable { get; set; }
        public double UsedMargin { get; set; }
        public double MarginInit { get; set; }
        public double PnL { get; set; }
        public double OpenPositionsCount { get; set; }
        public double MarginUsageLevel { get; set; }
        public bool IsLive { get; set; }

        public static AccountsStatReportEntity Create(IAccountsStatReport s)
        {
            return new AccountsStatReportEntity
            {
                BaseAssetId = s.BaseAssetId,
                AccountId = s.AccountId,
                ClientId = s.ClientId,
                TradingConditionId = s.TradingConditionId,
                Balance = (double)s.Balance,
                WithdrawTransferLimit = (double)s.WithdrawTransferLimit,
                MarginCall = (double)s.MarginCall,
                StopOut = (double)s.StopOut,
                TotalCapital = (double)s.TotalCapital,
                FreeMargin = (double)s.FreeMargin,
                MarginAvailable = (double)s.MarginAvailable,
                UsedMargin = (double)s.UsedMargin,
                MarginInit = (double)s.MarginInit,
                PnL = (double)s.PnL,
                OpenPositionsCount = (double)s.OpenPositionsCount,
                MarginUsageLevel = (double)s.MarginUsageLevel,
                IsLive = s.IsLive
            };
        }
    }
}