using System.ComponentModel;

namespace MarginTrading.Contract.ClientContracts
{
    [DisplayName("Account info")]
    public class MarginTradingAccountClientContract
    {
        [DisplayName("Account id")]
        public string Id { get; set; }
        [DisplayName("Trading condition id")]
        public string TradingConditionId { get; set; }
        [DisplayName("Account base asset id")]
        public string BaseAssetId { get; set; }
        [DisplayName("Account balance")]
        public decimal Balance { get; set; }
        [DisplayName("Account Withdraw Transfer Limit")]
        public decimal WithdrawTransferLimit { get; set; }
        [DisplayName("Account margin call limit")]
        public decimal MarginCall { get; set; }
        [DisplayName("Account stopout limit")]
        public decimal StopOut { get; set; }
        [DisplayName("Account total capital")]
        public decimal TotalCapital { get; set; }
        [DisplayName("Account free margin")]
        public decimal FreeMargin { get; set; }
        [DisplayName("Account available margin")]
        public decimal MarginAvailable { get; set; }
        [DisplayName("Account used margin")]
        public decimal UsedMargin { get; set; }
        [DisplayName("Account init margin")]
        public decimal MarginInit { get; set; }
        [DisplayName("Account pnl")]
        public decimal PnL { get; set; }
        [DisplayName("Open positions count")]
        public int OpenPositionsCount { get; set; }
        [DisplayName("Account margin usage level")]
        public decimal MarginUsageLevel { get; set; }
        [DisplayName("Is account live (trading real money) or demo")]
        public bool IsLive { get; set; }
    }
}
