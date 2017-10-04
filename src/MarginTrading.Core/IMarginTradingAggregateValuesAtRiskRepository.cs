using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAggregateValuesAtRiskRepository
    {
		Task InsertOrUpdateAsync(string counterPartyId, decimal value);
    }
}
