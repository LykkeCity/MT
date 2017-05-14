using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface ISampleQuoteCacheService
	{
		Task InitializeAsync();

		Task RunCacheUpdateAsync();

		double? GetLatestUsdQuote(string asset, OrderDirection side);

		double[] GetMeanUsdQuoteVector(string asset);
	}
}