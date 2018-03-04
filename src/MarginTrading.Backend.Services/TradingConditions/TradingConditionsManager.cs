using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class TradingConditionsManager : IStartable
    {
        private readonly ITradingConditionRepository _repository;
        private readonly TradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IConsole _console;
        private readonly AccountGroupManager _accountGroupManager;
        private readonly IClientNotifyService _clientNotifyService;

        public TradingConditionsManager(
            ITradingConditionRepository repository,
            TradingConditionsCacheService tradingConditionsCacheService,
            IConsole console,
            AccountGroupManager accountGroupManager,
            IClientNotifyService clientNotifyService)
        {
            _repository = repository;
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _console = console;
            _accountGroupManager = accountGroupManager;
            _clientNotifyService = clientNotifyService;
        }

        public void Start()
        {
            UpdateAllTradingConditions().Wait();
        }

        public async Task<ITradingCondition> AddOrReplaceTradingConditionAsync(ITradingCondition tradingCondition)
        {
            var allTradingConditions = _tradingConditionsCacheService.GetAllTradingConditions();
            
            var defaultTradingCondition = allTradingConditions.FirstOrDefault(item => item.IsDefault);
            
            if (tradingCondition.IsDefault)
            {
                if (defaultTradingCondition != null && defaultTradingCondition.Id != tradingCondition.Id)
                {
                    await SetIsDefault(defaultTradingCondition, false);
                }
            }
            else if (defaultTradingCondition?.Id == tradingCondition.Id)
            {
                var firstNotDefaultCondition = allTradingConditions.FirstOrDefault(item => !item.IsDefault);

                if (firstNotDefaultCondition != null)
                {
                    await SetIsDefault(firstNotDefaultCondition, true);
                }
            }

            await _repository.AddOrReplaceAsync(tradingCondition);
            
            if (_tradingConditionsCacheService.GetTradingCondition(tradingCondition.Id) == null)
            {
                await _accountGroupManager.AddAccountGroupsForTradingCondition(tradingCondition.Id);
            }
            
            await UpdateAllTradingConditions();
            
            await _clientNotifyService.NotifyTradingConditionsChanged(tradingCondition.Id);

            return tradingCondition;
        }

        private async Task SetIsDefault(ITradingCondition tradingCondition, bool isDefault)
        {
            var existing = TradingCondition.Create(tradingCondition);
            existing.IsDefault = isDefault;
            await _repository.AddOrReplaceAsync(existing);
        }

        private async Task UpdateAllTradingConditions()
        {
            var tradingConditions = (await _repository.GetAllAsync()).ToList();

            _tradingConditionsCacheService.InitTradingConditionsCache(tradingConditions);
            _console.WriteLine($"InitTradingConditionsCache (trading conditions count:{tradingConditions.Count})");
        }
    }
}
