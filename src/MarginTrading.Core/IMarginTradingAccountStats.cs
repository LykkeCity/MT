using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAccountStats
    {
        string AccountId { get; set; }
        string BaseAssetId { get; set; }
        double MarginCall { get; set; }
        double StopOut { get; set; }
        double TotalCapital { get; set; }
        double FreeMargin { get; set; }
        double MarginAvailable { get; set; }
        double UsedMargin { get; set; }
        double MarginInit { get; set; }
        double PnL { get; set; }
        double OpenPositionsCount { get; set; }
        double MarginUsageLevel { get; set; }
    }
    public class MarginTradingAccountStats : IMarginTradingAccountStats
    {
        public string AccountId { get; set; }
        public string BaseAssetId { get; set; }
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
    }

    public interface IMarginTradingAccountStatsRepository
    {
        Task<IEnumerable<IMarginTradingAccountStats>> GetAllAsync();
        Task InsertOrReplaceBatchAsync(IEnumerable<IMarginTradingAccountStats> stats);
    }
}
