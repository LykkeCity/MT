using System;

namespace MarginTrading.AccountReportsBroker.Repositories.Models
{
    public class AccountsStatReport : IAccountsStatReport
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string BaseAssetId { get; set; }
        public string AccountId { get; set; }
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
        public string LegalEntity { get; set; }
    }
}
