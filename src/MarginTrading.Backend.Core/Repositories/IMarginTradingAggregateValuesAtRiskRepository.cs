using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingAggregateValuesAtRiskRepository
    {
		Task InsertOrUpdateAsync(string counterPartyId, decimal value);
    }
}
