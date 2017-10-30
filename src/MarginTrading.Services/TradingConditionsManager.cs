using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.RabbitMq;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.Services
{
    public class TradingConditionsManager : IStartable
    {
        private readonly IMarginTradingConditionRepository _repository;
        private readonly TradingConditionsCacheService _tradingConditionsService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly MarginSettings _marginSettings;
        private readonly IConsole _console;

        public TradingConditionsManager(
            IMarginTradingConditionRepository repository,
            TradingConditionsCacheService tradingConditionsService,
            IAccountsCacheService accountsCacheService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            MarginSettings marginSettings,
            IConsole console)
        {
            _repository = repository;
            _tradingConditionsService = tradingConditionsService;
            _accountsCacheService = accountsCacheService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _marginSettings = marginSettings;
            _console = console;
        }

        public void Start()
        {
            UpdateAllTradingConditions().Wait();
        }

        public async Task UpdateTradingConditions(string tradingConditionId = null, string accountId = null)
        {
            //TODO: for what???
            await UpdateAllTradingConditions();

            if (!string.IsNullOrEmpty(tradingConditionId))
            {
                string[] clientIds = _accountsCacheService.GetClientIdsByTradingConditionId(tradingConditionId, accountId).ToArray();

                if (clientIds.Length > 0)
                {
                    await _rabbitMqNotifyService.UserUpdates(true, false, clientIds);
                    _console.WriteLine($"send user updates to queue {QueueHelper.BuildQueueName(_marginSettings.RabbitMqQueues.UserUpdates.ExchangeName, _marginSettings.Env)}");
                }
            }
        }

        public async Task AddOrReplaceTradingConditionAsync(IMarginTradingCondition tradingCondition)
        {
            var allTradingConditions = (await _repository.GetAllAsync()).ToList();
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
            await UpdateTradingConditions(tradingCondition.Id);
        }

        private async Task SetIsDefault(IMarginTradingCondition tradingCondition, bool isDefault)
        {
            var existing = MarginTradingCondition.Create(tradingCondition);
            existing.IsDefault = isDefault;
            await _repository.AddOrReplaceAsync(existing);
        }

        private async Task UpdateAllTradingConditions()
        {
            var tradingConditions = (await _repository.GetAllAsync()).ToList();

            _tradingConditionsService.InitTradingConditionsCache(tradingConditions);
            _console.WriteLine($"InitTradingConditionsCache (trading conditins count:{tradingConditions.Count})");
        }
    }
}
