using System.Linq;
using System.Threading.Tasks;
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

        public TradingInstrumentsManager(
            TradingInstrumnentsCacheService accountAssetsCacheService,
            ITradingInstrumentsApi tradingInstruments,
            IConvertService convertService)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _tradingInstruments = tradingInstruments;
            _convertService = convertService;
        }

        public void Start()
        {
            UpdateInstrumentsCache().Wait();
        }

        public async Task UpdateInstrumentsCache()
        {
            var accountAssets = (await _tradingInstruments.List(string.Empty)).Select(i =>
                (ITradingInstrument) _convertService.Convert<TradingInstrumentContract, TradingInstrument>(i)).ToList();
            
            _accountAssetsCacheService.InitAccountAssetsCache(accountAssets);
        }
    }
}
