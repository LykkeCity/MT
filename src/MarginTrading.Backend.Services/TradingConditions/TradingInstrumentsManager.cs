using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    [UsedImplicitly]
    public class TradingInstrumentsManager : ITradingInstrumentsManager
    {
        private readonly TradingInstrumnentsCacheService _accountAssetsCacheService;
        private readonly ITradingInstrumentsApi _tradingInstruments;
        private readonly IConvertService _convertService;
        private readonly IConsole _console;

        public TradingInstrumentsManager(
            TradingInstrumnentsCacheService accountAssetsCacheService,
            ITradingInstrumentsApi tradingInstruments,
            IConvertService convertService,
            IConsole console)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _tradingInstruments = tradingInstruments;
            _convertService = convertService;
            _console = console;
        }

        public void Start()
        {
            UpdateTradingInstrumentsCache().Wait();
        }

        public async Task UpdateTradingInstrumentsCache()
        {
            _console.WriteLine($"Started {nameof(UpdateTradingInstrumentsCache)}");
            
            var instruments = await _tradingInstruments.List(string.Empty);

            if (instruments != null)
            {
                _accountAssetsCacheService.InitAccountAssetsCache(instruments.Select(i =>
                        (ITradingInstrument) _convertService.Convert<TradingInstrumentContract, TradingInstrument>(i))
                    .ToList());
            }

            _console.WriteLine($"Finished {nameof(UpdateTradingInstrumentsCache)}. Count: {instruments?.Count ?? 0}");
        }
    }
}
