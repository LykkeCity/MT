// Copyright (c) 2019 Lykke Corp.

using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    [UsedImplicitly]
    public class TradingConditionsManager : IStartable, ITradingConditionsManager
    {
        private readonly ITradingConditionsApi _tradingConditions;
        private readonly TradingConditionsCacheService _tradingConditionsCacheService;
        private readonly IConsole _console;
        private readonly IConvertService _convertService;

        public TradingConditionsManager(
            ITradingConditionsApi tradingConditions,
            TradingConditionsCacheService tradingConditionsCacheService,
            IConsole console,
            IConvertService convertService)
        {
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _console = console;
            _convertService = convertService;
            _tradingConditions = tradingConditions;
        }

        public void Start()
        {
            InitTradingConditionsAsync().Wait();
        }

        public async Task InitTradingConditionsAsync()
        {
            _console.WriteLine($"Started {nameof(InitTradingConditionsAsync)}");

            var tradingConditions = await _tradingConditions.List();

            if (tradingConditions != null)
            {
                _tradingConditionsCacheService.InitTradingConditionsCache(tradingConditions.Select(t =>
                        (ITradingCondition) _convertService.Convert<TradingConditionContract, TradingCondition>(t))
                    .ToList());
            }

            _console.WriteLine(
                $"Finished {nameof(InitTradingConditionsAsync)}. Count:{tradingConditions?.Count ?? 0})");
        }
    }
}
