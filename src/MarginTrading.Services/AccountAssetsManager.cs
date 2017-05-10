using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class AccountAssetsManager : IStartable
    {
        private readonly AccountAssetsCacheService _accountAssetsCacheService;
        private readonly IMarginTradingAccountAssetRepository _repository;

        public AccountAssetsManager(
            AccountAssetsCacheService accountAssetsCacheService,
            IMarginTradingAccountAssetRepository accountAssetRepository)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _repository = accountAssetRepository;
        }

        public void Start()
        {
            UpdateAccountAssetsCache().Wait();
        }

        public async Task UpdateAccountAssetsCache()
        {
            var accountAssets = (await _repository.GetAllAsync()).ToList();
            _accountAssetsCacheService.InitAccountAssetsCache(accountAssets);
        }

        public async Task AssignInstruments(string tradingConditionId, string baseAssetId, string[] instruments)
        {
            await _repository.AssignInstruments(tradingConditionId, baseAssetId, instruments);
            await UpdateAccountAssetsCache();
        }

        public async Task AddOrReplaceAccountAssetAsync(MarginTradingAccountAsset model)
        {
            await _repository.AddOrReplaceAsync(model);
            await UpdateAccountAssetsCache();
        }
    }
}
