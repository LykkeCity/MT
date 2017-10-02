using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAccountStats
    {
        string AccountId { get; }
        string BaseAssetId { get; }
        decimal MarginCall { get; }
        decimal StopOut { get; }
        decimal TotalCapital { get; }
        decimal FreeMargin { get; }
        decimal MarginAvailable { get; }
        decimal UsedMargin { get; }
        decimal MarginInit { get; }
        decimal PnL { get; }
        decimal OpenPositionsCount { get; }
        decimal MarginUsageLevel { get; }
    }
    public class MarginTradingAccountStats : IMarginTradingAccountStats
    {
        public string AccountId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal PnL { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }
    }

    public interface IMarginTradingAccountStatsRepository
    {
        Task<IEnumerable<IMarginTradingAccountStats>> GetAllAsync();
        Task InsertOrReplaceBatchAsync(IEnumerable<IMarginTradingAccountStats> stats);
    }
}
