// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class AssetPairsCacheTests
    {
        [Test]
        public void Test_AssetPairs_WithDuplicated_Base_And_Quote_Assets_Works()
        {
            //arrange
            var pairs = new[]
            {
                CreateAssetPair("1", "EUR", "USD"), 
                CreateAssetPair("2", "CHF", "USD"),
                CreateAssetPair("3", "EUR", "USD"), 
                CreateAssetPair("4", "USD", "EUR")
            };
            
            var cache = new AssetPairsCache();

            //act
            (cache as IAssetPairsInitializableCache).InitPairsCache(pairs.ToDictionary(p => p.Id));
            
            //assert
            Assert.AreEqual("1", cache.FindAssetPair("EUR", "USD", "Default").Id);
        }

        private IAssetPair CreateAssetPair(string id, string baseAsset, string quoteAsset)
        {
            return new AssetPair(
                id: id,
                name: id,
                baseAssetId: baseAsset,
                quoteAssetId: quoteAsset,
                accuracy: AssetPairConstants.Accuracy,
                marketId: AssetPairConstants.FxMarketId,
                legalEntity: "Default",
                basePairId: AssetPairConstants.BasePairId,
                matchingEngineMode: AssetPairConstants.MatchingEngineMode,
                stpMultiplierMarkupBid: AssetPairConstants.StpMultiplierMarkupBid,
                stpMultiplierMarkupAsk: AssetPairConstants.StpMultiplierMarkupAsk,
                isSuspended: false,
                isFrozen: false,
                isDiscontinued: false
            );
        }
    }
}