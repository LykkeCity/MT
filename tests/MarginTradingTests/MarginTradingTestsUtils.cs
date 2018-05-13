using System.Collections.Generic;
using AzureStorage.Tables;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.Asset;
using MarginTrading.SettingsService.Contracts.Enums;
using Moq;
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

        public static MarginTradingAccountsRepository GetPopulatedAccountsRepository(List<MarginTradingAccount> accounts)
        {
            var accountRepository = new MarginTradingAccountsRepository(new NoSqlTableInMemory<MarginTradingAccountEntity>());

            foreach (var account in accounts)
            {
                accountRepository.AddAsync(account).Wait();
            }

            return accountRepository;
        }

        public static AccountGroupRepository GetPopulatedAccountGroupRepository()
        {
            var accountGroupRepository = new AccountGroupRepository(new NoSqlTableInMemory<AccountGroupEntity>());

            var groups = new List<AccountGroup>
            {
                new AccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    MarginCall = 1.25M,
                    StopOut = 1.05M
                },
                new AccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "EUR",
                    MarginCall = 1.25M,
                    StopOut = 1.05M
                },
                new AccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "CHF",
                    MarginCall = 1.25M,
                    StopOut = 1.05M
                }
            };

            foreach (var group in groups)
            {
                accountGroupRepository.AddOrReplaceAsync(group).Wait();
            }

            return accountGroupRepository;
        }

        public static AccountAssetsPairsRepository GetPopulatedAccountAssetsRepository()
        {
            var repository = new AccountAssetsPairsRepository(new NoSqlTableInMemory<AccountAssetPairEntity>());
            var assets = new List<AccountAssetPair>
            {
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCCHF",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "EURUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30,
                    DealLimit = 1000000,
                    PositionLimit = 10000000
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCEUR",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCUSD",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30,
                    DealLimit = 10,
                    PositionLimit = 100
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCCHF",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "CHFJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "JPYUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "EURGBP",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "GBPUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "EUR",
                    Instrument = "BTCJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new AccountAssetPair
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "EUR",
                    Instrument = "BTCEUR",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
            };

            foreach (var asset in assets)
            {
                repository.AddOrReplaceAsync(asset).Wait();
            }

            return repository;
        }

        public static TradingConditionsRepository GetPopulatedMarginTradingConditionsRepository()
        {
            var tradingConditionsRepository = new TradingConditionsRepository(new NoSqlTableInMemory<TradingConditionEntity>());

            tradingConditionsRepository.AddOrReplaceAsync(new TradingCondition
            {
                Id = TradingConditionId,
                IsDefault = true,
                Name = "Default trading condition"
            }).Wait();

            return tradingConditionsRepository;
        }

        public static MatchingEngineRoutesRepository GetPopulatedMatchingEngineRoutesRepository()
        {
            var repository = new MatchingEngineRoutesRepository(new NoSqlTableInMemory<MatchingEngineRouteEntity>());

            return repository;
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
