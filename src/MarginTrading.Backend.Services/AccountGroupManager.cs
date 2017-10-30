using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
    public class AccountGroupManager : IStartable
    {
        private readonly AccountGroupCacheService _accountGroupCacheService;
        private readonly IMarginTradingAccountGroupRepository _repository;
        private readonly MarginSettings _settings;

        public AccountGroupManager(
            AccountGroupCacheService accountGroupCacheService,
            IMarginTradingAccountGroupRepository accountGroupRepository,
            MarginSettings settings)
        {
            _accountGroupCacheService = accountGroupCacheService;
            _repository = accountGroupRepository;
            _settings = settings;
        }

        public void Start()
        {
            UpdateAccountGroupCache().Wait();
        }

        public async Task UpdateAccountGroupCache()
        {
            var accountGroups = (await _repository.GetAllAsync()).ToList();
            _accountGroupCacheService.InitAccountGroupsCache(accountGroups);
        }

        public async Task AddAccountGroupsForTradingCondition(string tradingConditionId)
        {
            foreach (var asset in _settings.BaseAccountAssets)
            {
                await _repository.AddOrReplaceAsync(new MarginTradingAccountGroup
                {
                    BaseAssetId = asset,
                    MarginCall = LykkeConstants.DefaultMarginCall,
                    StopOut = LykkeConstants.DefaultStopOut,
                    TradingConditionId = tradingConditionId
                });
            }

            await UpdateAccountGroupCache();
        }

        public async Task AddOrReplaceAccountGroupAsync(IMarginTradingAccountGroup accountGroup)
        {
            await _repository.AddOrReplaceAsync(accountGroup);
            await UpdateAccountGroupCache();
        }
    }
}
