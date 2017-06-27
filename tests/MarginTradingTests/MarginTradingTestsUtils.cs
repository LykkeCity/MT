using System.Collections.Generic;
using AzureStorage.Tables;
using MarginTrading.AzureRepositories;
using MarginTrading.Core;

namespace MarginTradingTests
{
    public class MarginTradingTestsUtils
    {
        public const string TradingConditionId = "1";

        public static MarginTradingAssetsRepository GetPopulatedAssetsRepository()
        {
            var assetsRepository = new MarginTradingAssetsRepository(new NoSqlTableInMemory<MarginTradingAssetEntity>());

            var assets = new List<MarginTradingAsset>
            {
                new MarginTradingAsset
                {
                    Id = "EURUSD",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "USD"
                },
                new MarginTradingAsset
                {
                    Id = "BTCEUR",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "EUR"
                },
                new MarginTradingAsset
                {
                    Id = "BTCUSD",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "USD"
                },
                new MarginTradingAsset
                {
                    Id = "BTCCHF",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "CHF"
                },
                new MarginTradingAsset
                {
                    Id = "CHFJPY",
                    Accuracy = 3,
                    BaseAssetId = "CHF",
                    QuoteAssetId = "JPY"
                },
                new MarginTradingAsset
                {
                    Id = "USDCHF",
                    Accuracy = 3,
                    BaseAssetId = "USD",
                    QuoteAssetId = "CHF"
                },
                new MarginTradingAsset
                {
                    Id = "EURCHF",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "CHF"
                },
                new MarginTradingAsset
                {
                    Id = "BTCJPY",
                    Accuracy = 5,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "JPY"
                },
                new MarginTradingAsset
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
                    MarginCall = 0.8,
                    StopOut = 0.95
                },
                new MarginTradingAccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "EUR",
                    MarginCall = 0.8,
                    StopOut = 0.95
                },
                new MarginTradingAccountGroup
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "CHF",
                    MarginCall = 0.8,
                    StopOut = 0.95
                }
            };

            foreach (var group in groups)
            {
                accountGroupRepository.AddOrReplaceAsync(group).Wait();
            }

            return accountGroupRepository;
        }

        public static MarginTradingAccountAssetsRepository GetPopulatedAccountAssetsRepository()
        {
            var repository = new MarginTradingAccountAssetsRepository(new NoSqlTableInMemory<MarginTradingAccountAssetEntity>());
            var assets = new List<MarginTradingAccountAsset>
            {
                new MarginTradingAccountAsset
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCCHF",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new MarginTradingAccountAsset
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
                new MarginTradingAccountAsset
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCEUR",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new MarginTradingAccountAsset
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
                new MarginTradingAccountAsset
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "BTCCHF",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new MarginTradingAccountAsset
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "USD",
                    Instrument = "CHFJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new MarginTradingAccountAsset
                {
                    TradingConditionId = TradingConditionId,
                    BaseAssetId = "EUR",
                    Instrument = "BTCJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    DeltaAsk = 30,
                    DeltaBid = 30
                },
                new MarginTradingAccountAsset
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
