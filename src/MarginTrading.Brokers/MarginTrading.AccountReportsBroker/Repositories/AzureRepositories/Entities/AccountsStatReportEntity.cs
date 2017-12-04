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
                Balance = s.Balance,
                WithdrawTransferLimit = s.WithdrawTransferLimit,
                MarginCall = s.MarginCall,
                StopOut = s.StopOut,
                TotalCapital = s.TotalCapital,
                FreeMargin = s.FreeMargin,
                MarginAvailable = s.MarginAvailable,
                UsedMargin = s.UsedMargin,
                MarginInit = s.MarginInit,
                PnL = s.PnL,
                OpenPositionsCount = s.OpenPositionsCount,
                MarginUsageLevel = s.MarginUsageLevel,
                IsLive = s.IsLive
            };
        }
    }
}