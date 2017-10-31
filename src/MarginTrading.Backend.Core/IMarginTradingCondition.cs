using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingCondition
    {
        string Id { get; }
        string Name { get; }
        bool IsDefault { get; }
    }

    public class MarginTradingCondition : IMarginTradingCondition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }

        public static MarginTradingCondition Create(IMarginTradingCondition src)
        {
            return new MarginTradingCondition
            {
                Id = src.Id,
                Name = src.Name,
                IsDefault = src.IsDefault
            };
        }
    }

    public interface IMarginTradingConditionRepository
    {
        Task AddOrReplaceAsync(IMarginTradingCondition condition);
        Task<IMarginTradingCondition> GetAsync(string tradingConditionId);
        Task<IEnumerable<IMarginTradingCondition>> GetAllAsync();
    }
}
