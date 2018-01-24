using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Lykke.Service.Assets.Client;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.AssetPairs
{
    public class AssetPairsManager : IStartable
    {
        private readonly IAssetsService _assets;
        private readonly AssetPairsCache _assetPairsCache;

        public AssetPairsManager(IAssetsService repository,
            AssetPairsCache assetPairsCache)
        {
            _assets = repository;
            _assetPairsCache = assetPairsCache;
        }

        public void Start()
        {
            UpdateInstrumentsCache().Wait();
        }

        public async Task UpdateInstrumentsCache()
        {
            var instruments = (await _assets.AssetPairGetAllAsync())
                .ToDictionary(
                    a => a.Id,
                    a => (IAssetPair) new AssetPair
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Accuracy = a.Accuracy,
                        BaseAssetId = a.BaseAssetId,
                        QuoteAssetId = a.QuotingAssetId
                    });
            
            _assetPairsCache.InitInstrumentsCache(instruments);
        }
    }
}
