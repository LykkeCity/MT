using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AccountAssetsManager
    {
        private readonly AccountAssetsCacheService _accountAssetsCacheService;
        private readonly IMarginTradingAccountAssetRepository _repository;
        private readonly MarginSettings _settings;

        public AccountAssetsManager(
            AccountAssetsCacheService accountAssetsCacheService,
            IMarginTradingAccountAssetRepository accountAssetRepository,
            MarginSettings settings)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _repository = accountAssetRepository;
            _settings = settings;
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
            var defaults = _settings.DefaultAccountAssetsSettings ?? new AccountAssetsSettings();
            await _repository.AssignInstruments(tradingConditionId, baseAssetId, instruments, defaults);
            await UpdateAccountAssetsCache();
        }

        public async Task AddOrReplaceAccountAssetAsync(MarginTradingAccountAsset model)
        {
            await _repository.AddOrReplaceAsync(model);
            await UpdateAccountAssetsCache();
        }
    }
}
