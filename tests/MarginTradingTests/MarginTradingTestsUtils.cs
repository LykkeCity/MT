using System.Collections.Generic;
using AzureStorage.Tables;
using MarginTrading.AzureRepositories;
using MarginTrading.Core;

namespace MarginTradingTests
{
    public class MarginTradingTestsUtils
    {
        public const string TradingConditionId = "1";

        public static AssetPairsRepository GetPopulatedAssetsRepository()
        {
            var assetsRepository = new AssetPairsRepository(new NoSqlTableInMemory<AssetPairEntity>());

            var assets = new List<AssetPair>
            {
                new AssetPair
                {
                    Id = "EURUSD",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "USD"
                },
                new AssetPair
                {
                    Id = "BTCEUR",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "EUR"
                },
                new AssetPair
                {
                    Id = "BTCUSD",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "USD"
                },
                new AssetPair
                {
                    Id = "BTCCHF",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "CHF"
                },
                new AssetPair
                {
                    Id = "CHFJPY",
                    Accuracy = 3,
                    BaseAssetId = "CHF",
                    QuoteAssetId = "JPY"
                },
                new AssetPair
                {
                    Id = "USDCHF",
                    Accuracy = 3,
                    BaseAssetId = "USD",
                    QuoteAssetId = "CHF"
                },
                new AssetPair
                {
                    Id = "EURCHF",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "CHF"
                },
                new AssetPair
                {
                    Id = "BTCJPY",
                    Accuracy = 5,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "JPY"
                },
                new AssetPair
                {
                    Id = "EURJPY",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "JPY"
                }
            };

            foreach (var asset in assets)
            {
                assetsRepository.AddAsync(asset).Wait();
            }

            return assetsRepository;
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

        public static MarginTradingAccountGroupRepository GetPopulatedAccountGroupRepository()
        {
            var accountGroupRepository = new MarginTradingAccountGroupRepository(new NoSqlTableInMemory<MarginTradingAccountGroupEntity>());

            var groups = new List<MarginTradingAccountGroup>
            {
                new MarginTradingAccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    MarginCall = 1.25M,
                    StopOut = 1.05M
                },
                new MarginTradingAccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "EUR",
                    MarginCall = 1.25M,
                    StopOut = 1.05M
                },
                new MarginTradingAccountGroup
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
                }
            };

            foreach (var asset in assets)
            {
                repository.AddOrReplaceAsync(asset).Wait();
            }

            return repository;
        }

        public static MarginTradingConditionsRepository GetPopulatedMarginTradingConditionsRepository()
        {
            var tradingConditionsRepository = new MarginTradingConditionsRepository(new NoSqlTableInMemory<MarginTradingConditionEntity>());

            tradingConditionsRepository.AddOrReplaceAsync(new MarginTradingCondition
            {
                Id = TradingConditionId,
                IsDefault = true,
                Name = "Default trading condition"
            }).Wait();

            return tradingConditionsRepository;
        }

        public static MarginTradingWatchListsRepository GetPopulatedMarginTradingWatchListsRepository()
        {
            var repository = new MarginTradingWatchListsRepository(new NoSqlTableInMemory<MarginTradingWatchListEntity>());

            return repository;
        }

        public static MatchingEngineRoutesRepository GetPopulatedMatchingEngineRoutesRepository()
        {
            var repository = new MatchingEngineRoutesRepository(new NoSqlTableInMemory<MatchingEngineRouteEntity>());

            return repository;
        }
    }
}
