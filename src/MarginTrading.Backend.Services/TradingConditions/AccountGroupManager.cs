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
        private readonly MarginTradingSettings _settings;
        private readonly IClientNotifyService _clientNotifyService;

        public AccountGroupManager(
            AccountGroupCacheService accountGroupCacheService,
            IAccountGroupRepository accountGroupRepository,
            MarginTradingSettings settings,
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
    }
}
