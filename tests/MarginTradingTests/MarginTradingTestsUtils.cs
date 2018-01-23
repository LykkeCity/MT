using System.Collections.Generic;
using System.Threading;
using AzureStorage.Tables;
using Lykke.Service.Assets.Client;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.TradingConditions;
using Microsoft.Rest;
using Moq;
using AssetPair = Lykke.Service.Assets.Client.Models.AssetPair;

namespace MarginTradingTests
{
    public class MarginTradingTestsUtils
    {
        public const string TradingConditionId = "1";

        public static IAssetsService GetPopulatedAssetsService()
        {
            var assetsService = new Mock<IAssetsService>();

            var assets = new List<AssetPair>
            {
                new AssetPair
                {
                    Id = "EURUSD",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuotingAssetId = "USD"
                },
                new AssetPair
                {
                    Id = "BTCEUR",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuotingAssetId = "EUR"
                },
                new AssetPair
                {
                    Id = "BTCUSD",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuotingAssetId = "USD"
                },
                new AssetPair
                {
                    Id = "BTCCHF",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuotingAssetId = "CHF"
                },
                new AssetPair
                {
                    Id = "CHFJPY",
                    Accuracy = 3,
                    BaseAssetId = "CHF",
                    QuotingAssetId = "JPY"
                },
                new AssetPair
                {
                    Id = "USDCHF",
                    Accuracy = 3,
                    BaseAssetId = "USD",
                    QuotingAssetId = "CHF"
                },
                new AssetPair
                {
                    Id = "EURCHF",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuotingAssetId = "CHF"
                },
                new AssetPair
                {
                    Id = "BTCJPY",
                    Accuracy = 5,
                    BaseAssetId = "BTC",
                    QuotingAssetId = "JPY"
                },
                new AssetPair
                {
                    Id = "EURJPY",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuotingAssetId = "JPY"
                }
            };

            var result = new HttpOperationResponse<IList<AssetPair>> {Body = assets};

            assetsService
                .Setup(s => s.AssetPairGetAllWithHttpMessagesAsync(It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(result);

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
    }
}
