using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
    public class AccountAssetsManager
    {
        private readonly AccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAccountAssetPairsRepository _pairsRepository;
        private readonly MarginSettings _settings;

        public AccountAssetsManager(
            AccountAssetsCacheService accountAssetsCacheService,
            IAccountAssetPairsRepository accountAssetPairsRepository,
            MarginSettings settings)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _pairsRepository = accountAssetPairsRepository;
            _settings = settings;
        }

        public void Start()
        {
            UpdateAccountAssetsCache().Wait();
        }

        public async Task UpdateAccountAssetsCache()
        {
            var accountAssets = (await _pairsRepository.GetAllAsync()).ToList();
            _accountAssetsCacheService.InitAccountAssetsCache(accountAssets);
        }

        public async Task AssignInstruments(string tradingConditionId, string baseAssetId, string[] instruments)
        {
            var defaults = _settings.DefaultAccountAssetsSettings ?? new AccountAssetsSettings();
            await _pairsRepository.AssignAssetPairs(tradingConditionId, baseAssetId, instruments, defaults);
            await UpdateAccountAssetsCache();
        }

        public async Task AddOrReplaceAccountAssetAsync(AccountAssetPair model)
        {
            await _pairsRepository.AddOrReplaceAsync(model);
            await UpdateAccountAssetsCache();
        }
    }
}
