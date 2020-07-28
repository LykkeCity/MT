// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    [UsedImplicitly]
    public class TradingConditionsManager : IStartable, ITradingConditionsManager
    {
        private readonly ITradingConditionsApi _tradingConditions;
        private readonly TradingConditionsCacheService _tradingConditionsCacheService;
        private readonly ILog _log;
        private readonly IConvertService _convertService;

        public TradingConditionsManager(
            ITradingConditionsApi tradingConditions,
            TradingConditionsCacheService tradingConditionsCacheService,
            ILog log,
            IConvertService convertService)
        {
            _tradingConditionsCacheService = tradingConditionsCacheService;
            _log = log;
            _convertService = convertService;
            _tradingConditions = tradingConditions;
        }

        public void Start()
        {
            InitTradingConditionsAsync().Wait();
        }

        public async Task InitTradingConditionsAsync()
        {
            await _log.WriteInfoAsync(nameof(InitTradingConditionsAsync), nameof(TradingConditionsManager), 
                $"Started {nameof(InitTradingConditionsAsync)}");

            var tradingConditions = await _tradingConditions.List();

            if (tradingConditions != null)
            {
                _tradingConditionsCacheService.InitTradingConditionsCache(tradingConditions.Select(t =>
                        (ITradingCondition) _convertService.Convert<TradingConditionContract, TradingCondition>(t))
                    .ToList());
            }

            await _log.WriteInfoAsync(nameof(InitTradingConditionsAsync), nameof(TradingConditionsManager),
                $"Finished {nameof(InitTradingConditionsAsync)}. Count:{tradingConditions?.Count ?? 0})");
        }
    }
}
