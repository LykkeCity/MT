using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class InstrumentsManager : IStartable
    {
        private readonly IMarginTradingAssetPairsRepository _repository;
        private readonly AssetPairsCache _assetPairsCache;

        public InstrumentsManager(IMarginTradingAssetPairsRepository repository,
            AssetPairsCache assetPairsCache)
        {
            _repository = repository;
            _assetPairsCache = assetPairsCache;
        }

        public void Start()
        {
            UpdateInstrumentsCache().Wait();
        }

        public async Task UpdateInstrumentsCache()
        {
            var instruments = (await _repository.GetAllAsync()).ToDictionary(x => x.Id);
            _assetPairsCache.InitInstrumentsCache(instruments);
        }
    }
}
