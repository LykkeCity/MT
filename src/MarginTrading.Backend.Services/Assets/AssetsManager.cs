using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Lykke.Service.Assets.Client;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Assets
{
    public class AssetsManager : IStartable
    {
        private readonly IAssetsService _assetsService;
        private readonly AssetsCache _assetsCache;

        public AssetsManager(IAssetsService assetsService,
            AssetsCache assetsCache)
        {
            _assetsService = assetsService;
            _assetsCache = assetsCache;
        }

        public void Start()
        {
            UpdateCache().Wait();
        }

        public async Task UpdateCache()
        {
            var assets = (await _assetsService.AssetGetAllAsync())
                .ToDictionary(
                    a => a.Id,
                    a => (IAsset) new Asset
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Accuracy = a.Accuracy
                    });
            
            _assetsCache.Init(assets);
        }
    }
}
