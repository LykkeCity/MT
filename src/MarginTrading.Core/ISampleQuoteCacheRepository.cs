using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface ISampleQuoteCacheRepository
	{
		Task Backup(ISampleQuoteCache sampleQuoteCache);

		Task<ISampleQuoteCache> Restore();

		Task SaveSettings(int maxCount, long sampleInterval);

		Task<int> GetMaxCount();

		Task<long> GetSampleInterval();

		bool Any();
	}
}
