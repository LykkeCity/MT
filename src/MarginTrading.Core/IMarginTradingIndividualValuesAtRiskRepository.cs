using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IMarginTradingIndividualValuesAtRiskRepository
	{
		Task InsertOrUpdateAsync(string counterPartyId, string assetId, decimal value);
	}
}
