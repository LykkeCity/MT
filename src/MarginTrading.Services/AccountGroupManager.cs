using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class AccountGroupManager : IStartable
    {
        private readonly AccountGroupCacheService _accountGroupCacheService;
        private readonly IMarginTradingAccountGroupRepository _repository;

        public AccountGroupManager(
            AccountGroupCacheService accountGroupCacheService,
            IMarginTradingAccountGroupRepository accountGroupRepository)
        {
            _accountGroupCacheService = accountGroupCacheService;
            _repository = accountGroupRepository;
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
            foreach (var asset in LykkeConstants.BaseAssets)
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
