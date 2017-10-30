using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services
{
    public class InstrumentsManager : IStartable
    {
        private readonly IAssetPairsRepository _repository;
        private readonly AssetPairsCache _assetPairsCache;

        public InstrumentsManager(IAssetPairsRepository repository,
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
