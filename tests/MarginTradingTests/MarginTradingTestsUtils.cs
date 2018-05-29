using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.Asset;
using MarginTrading.SettingsService.Contracts.Enums;
using MarginTrading.SettingsService.Contracts.TradingConditions;
using Moq;
using NUnit.Framework.Constraints;
using AssetPairContract = MarginTrading.SettingsService.Contracts.AssetPair.AssetPairContract;

namespace MarginTradingTests
{
    public class MarginTradingTestsUtils
    {
        public const string TradingConditionId = "1";

        public static IAssetsApi GetPopulatedAssets()
        {
            var assetsService = new Mock<IAssetsApi>();

            var assets = new List<AssetContract>
            {
                new AssetContract
                {
                    Id = "BTC",
                    Name = "BTC",
                    Accuracy = 8
                }
            };

            assetsService.Setup(s => s.List()).ReturnsAsync(assets);

            return assetsService.Object;
        }

        public static IAccountsApi GetPopulatedAccountsApi(List<MarginTradingAccount> accounts)
        {
            var list = accounts.Select(a => new AccountContract(a.Id, a.ClientId, a.TradingConditionId, a.BaseAssetId,
                a.Balance, a.WithdrawTransferLimit, a.LegalEntity, a.IsDisabled, DateTimeOffset.UtcNow)).ToList();
            return Mock.Of<IAccountsApi>(a => a.List() == Task.FromResult(list));
        }

        public static ITradingInstrumentsApi GetPopulatedTradingInstruments()
        {
            var instruments = new List<TradingInstrumentContract>
            {
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCCHF",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30,
                    DealMaxLimit = 1000000,
                    PositionLimit = 10000000
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCEUR",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCUSD",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30,
                    DealMaxLimit = 10,
                    PositionLimit = 100
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "CHFJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "JPYUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURGBP",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "GBPUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30
                }
            };

            var mock = new Mock<ITradingInstrumentsApi>();
            mock.Setup(s => s.List(It.IsAny<string>())).ReturnsAsync(instruments);

            return mock.Object;
        }

        public static ITradingConditionsApi GetPopulatedTradingConditions()
        {
            var defaultTc = new TradingConditionContract
            {
                Id = TradingConditionId,
                IsDefault = true,
                Name = "Default trading condition",
                BaseAssets = new List<string> {"USD", "EUR", "CHF"},
                MarginCall1 = 1.25M,
                MarginCall2 = 1.15M,
                StopOut = 1.05M
            };

            var mock = new Mock<ITradingConditionsApi>();
            mock.Setup(s => s.List()).ReturnsAsync(new List<TradingConditionContract> {defaultTc});

            return mock.Object;
        }

        public static ITradingRoutesApi GetPopulatedMatchingEngineRoutes()
        {
            return Mock.Of<ITradingRoutesApi>();
        }

        public static IAssetPairsApi GetPopulatedAssetPairs()
        {
            var assetPairs = new List<AssetPairContract>
            {
                new AssetPairContract
                {
                    Id = "EURUSD",
                    Name = "EURUSD",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "USD",
                },
                new AssetPairContract
                {
                    Id = "BTCEUR",
                    Name = "BTCEUR",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "EUR"
                },
                new AssetPairContract
                {
                    Id = "BTCUSD",
                    Name = "BTCUSD",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "USD"
                },
                new AssetPairContract
                {
                    Id = "BTCCHF",
                    Name = "BTCCHF",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "CHF"
                },
                new AssetPairContract
                {
                    Id = "CHFJPY",
                    Name = "CHFJPY",
                    Accuracy = 3,
                    BaseAssetId = "CHF",
                    QuoteAssetId = "JPY"
                },
                new AssetPairContract
                {
                    Id = "USDCHF",
                    Name = "USDCHF",
                    Accuracy = 3,
                    BaseAssetId = "USD",
                    QuoteAssetId = "CHF"
                },
                new AssetPairContract
                {
                    Id = "EURCHF",
                    Name = "EURCHF",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "CHF"
                },
                new AssetPairContract
                {
                    Id = "BTCJPY",
                    Name = "BTCJPY",
                    Accuracy = 5,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "JPY"
                },
                new AssetPairContract
                {
                    Id = "EURJPY",
                    Name = "EURJPY",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "JPY"
                },
                new AssetPairContract
                {
                    Id = "JPYUSD",
                    Name = "JPYUSD",
                    Accuracy = 3,
                    BaseAssetId = "JPY",
                    QuoteAssetId = "USD"
                },
                new AssetPairContract
                {
                    Id = "EURGBP",
                    Name = "EURGBP",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "GBP"
                },
                new AssetPairContract
                {
                    Id = "GBPUSD",
                    Name = "GBPUSD",
                    Accuracy = 3,
                    BaseAssetId = "GBP",
                    QuoteAssetId = "USD"
                },
            };

            foreach (var pair in assetPairs)
            {
                pair.LegalEntity = "LYKKETEST";
                pair.MatchingEngineMode = MatchingEngineModeContract.MarketMaker;
                pair.StpMultiplierMarkupAsk = 1;
                pair.StpMultiplierMarkupBid = 1;
            }

            var mock = new Mock<IAssetPairsApi>();
            mock.Setup(m => m.List(It.IsAny<string>(), It.IsAny<MatchingEngineModeContract?>()))
                .ReturnsAsync(assetPairs);

            return mock.Object;
        }
    }
}