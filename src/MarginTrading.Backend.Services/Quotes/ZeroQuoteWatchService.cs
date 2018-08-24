using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.SettingsService.Contracts.AssetPair;

namespace MarginTrading.Backend.Services.Quotes
{
    public class ZeroQuoteWatchService: IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICqrsSender _cqrsSender;
        private readonly IIdentityGenerator _identityGenerator;

        public ZeroQuoteWatchService(
            IAssetPairsCache assetPairsCache,
            ICqrsSender cqrsSender,
            IIdentityGenerator identityGenerator)
        {
            _assetPairsCache = assetPairsCache;
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
        }
        
        public void ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(ea.BidAskPair.Instrument);
            if (assetPair == null)
            {
                return;
            }
            
            if (ea.BidAskPair.Ask == 0 || ea.BidAskPair.Bid == 0)
            {
                if (!assetPair.IsSuspended)
                {
                    assetPair.IsSuspended = true;//todo apply changed to trading engine
                    _cqrsSender.SendCommandToSettingsService(new SuspendAssetPairCommand
                    {
                        AssetPairId = assetPair.Id,
                        OperationId = _identityGenerator.GenerateGuid(),
                    });
                }
            }
            else
            {
                if (assetPair.IsSuspended)
                {
                    assetPair.IsSuspended = false;//todo apply changed to trading engine
                    _cqrsSender.SendCommandToSettingsService(new UnsuspendAssetPairCommand
                    {
                        AssetPairId = assetPair.Id,
                        OperationId = _identityGenerator.GenerateGuid(),
                    });   
                }
            }
        }

        public int ConsumerRank => 100;
    }
}