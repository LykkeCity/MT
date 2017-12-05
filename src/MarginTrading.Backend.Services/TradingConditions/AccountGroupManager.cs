using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class AccountGroupManager : IStartable
    {
        private readonly AccountGroupCacheService _accountGroupCacheService;
        private readonly IAccountGroupRepository _repository;
        private readonly MarginSettings _settings;
        private readonly IClientNotifyService _clientNotifyService;

        public AccountGroupManager(
            AccountGroupCacheService accountGroupCacheService,
            IAccountGroupRepository accountGroupRepository,
            MarginSettings settings,
            IClientNotifyService clientNotifyService)
        {
            _accountGroupCacheService = accountGroupCacheService;
            _repository = accountGroupRepository;
            _settings = settings;
            _clientNotifyService = clientNotifyService;
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
                await _repository.AddOrReplaceAsync(new AccountGroup
                {
                    BaseAssetId = asset,
                    MarginCall = LykkeConstants.DefaultMarginCall,
                    StopOut = LykkeConstants.DefaultStopOut,
                    TradingConditionId = tradingConditionId
                });
            }

            await UpdateAccountGroupCache();
        }

        public async Task<IAccountGroup> AddOrReplaceAccountGroupAsync(IAccountGroup accountGroup)
        {
            await _repository.AddOrReplaceAsync(accountGroup);
            await UpdateAccountGroupCache();

            await _clientNotifyService.NotifyTradingConditionsChanged(accountGroup.TradingConditionId);

            return accountGroup;
        }
    }
}
