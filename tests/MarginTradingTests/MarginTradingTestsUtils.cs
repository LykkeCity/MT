// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Snow.Mdm.Contracts.Api;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Responses;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.TradingConditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;
using MarginTrading.AccountsManagement.Contracts.Api;
using Moq;
using AssetPairContract = MarginTrading.AssetService.Contracts.AssetPair.AssetPairContract;
using MarginTrading.AssetService.Contracts.LegacyAsset;

namespace MarginTradingTests
{
    [UsedImplicitly]
    public static class MarginTradingTestsUtils
    {
        public const string TradingConditionId = "1";
        public const string DefaultMarket = "DefaultMarket";
        

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
            var list = accounts.Select(a => new AccountContract
            {
                Id = a.Id,
                ClientId = a.ClientId,
                TradingConditionId = a.TradingConditionId,
                BaseAssetId = a.BaseAssetId,
                Balance = a.Balance,
                WithdrawTransferLimit = a.WithdrawTransferLimit,
                LegalEntity = a.LegalEntity,
                IsDisabled = a.IsDisabled,
                ModificationTimestamp = DateTime.UtcNow,
                IsWithdrawalDisabled = a.IsWithdrawalDisabled,
                IsDeleted = false,
                AdditionalInfo = "{}"
            }).ToList();

            var mock = new Mock<IAccountsApi>();
            mock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(list);

            // emulate disposable capital exhaustion after 1st withdrawal
            // however, it is important to note that SetupSequence API is
            // known to have issues in multi-threaded environment
            mock.SetupSequence(x =>
                    x.GetDisposableCapital(
                        It.IsIn(accounts.Select(a => a.Id)),
                        It.IsAny<GetDisposableCapitalRequest>()))
                .ReturnsAsync(1000)
                .ReturnsAsync(0)
                .ReturnsAsync(0);
            
            return mock.Object;
        }
        
        public static IAccountBalanceHistoryApi GetPopulatedAccountBalanceHistoryApi()
        {
            return Mock.Of<IAccountBalanceHistoryApi>(a =>
                a.ByDate(It.IsAny<DateTime>(), It.IsAny<DateTime>()) == Task.FromResult(new Dictionary<string, AccountBalanceChangeLightContract[]>()));
        }


        public static IBrokerSettingsApi GetBrokerSettingsApi(string brokerId, bool productComplexityEnabled = false)
        {
            var resp = new GetBrokerSettingsByIdResponse
            {
                BrokerSettings = new BrokerSettingsContract
                {
                    BrokerId = brokerId,
                    ProductComplexityWarningEnabled = productComplexityEnabled
                },
                ErrorCode = BrokerSettingsErrorCodesContract.None
            };

            var api = new Mock<IBrokerSettingsApi>();

            api.Setup(x => x.GetByIdAsync(brokerId)).ReturnsAsync(resp);

            return api.Object;
        }


        public static IFeatureManager GetFeatureManager(string brokerId, IBrokerSettingsApi api)
        {
            var services = new ServiceCollection();
            services.AddSingleton(api);
            services.AddFeatureManagement(brokerId);

            return services.BuildServiceProvider().GetRequiredService<IFeatureManager>();
        }


        public static ITradingInstrumentsApi GetPopulatedTradingInstruments()
        {
            var instruments = new List<TradingInstrumentContract>
            {
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCCHF",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 10,
                    MaintenanceLeverage = 15,
                    MarginRatePercent = 6.67M
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURUSD",
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 1000000,
                    PositionLimit = 10000000,
                    InitLeverage = 100,
                    MaintenanceLeverage = 150,
                    MarginRatePercent = 0.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BYNUSD",
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 1000000,
                    PositionLimit = 10000000,
                    MaxPositionNotional = 1000,
                    InitLeverage = 100,
                    MaintenanceLeverage = 150,
                    MarginRatePercent = 0.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURRUB",
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 1000000,
                    PositionLimit = 10000000,
                    InitLeverage = 100,
                    MaintenanceLeverage = 100,
                    MarginRatePercent = 1,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCEUR",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 10,
                    MaintenanceLeverage = 15,
                    MarginRatePercent = 6.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCUSD",
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 10,
                    PositionLimit = 100,
                    InitLeverage = 10,
                    MaintenanceLeverage = 15,
                    MarginRatePercent = 6.67M
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "CHFJPY",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 10,
                    MaintenanceLeverage = 15,
                    MarginRatePercent = 6.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "JPYUSD",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 100,
                    MaintenanceLeverage = 150,
                    MarginRatePercent = 0.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURGBP",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 100,
                    MaintenanceLeverage = 150,
                    MarginRatePercent = 0.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "GBPUSD",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 100,
                    MaintenanceLeverage = 150,
                    MarginRatePercent = 0.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCJPY",
                    Delta = 30,
                    ShortPosition = true,
                    InitLeverage = 100,
                    MaintenanceLeverage = 150,
                    MarginRatePercent = 0.67M,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BLINDR",
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 10,
                    PositionLimit = 100,
                    InitLeverage = 10,
                    MaintenanceLeverage = 15,
                    MarginRatePercent = 6.67M
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
                MarginCall2 = 1.11M,
                StopOut = 1M
            };

            var mock = new Mock<ITradingConditionsApi>();
            mock.Setup(s => s.List(It.IsAny<bool?>())).ReturnsAsync(new List<TradingConditionContract> {defaultTc});

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
                    Id = "BYNUSD",
                    Name = "BYNUSD",
                    Accuracy = 5,
                    BaseAssetId = "BYN",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "EURUSD",
                    Name = "EURUSD",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "BTCEUR",
                    Name = "BTCEUR",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "EUR",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "BTCUSD",
                    Name = "BTCUSD",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "BTCCHF",
                    Name = "BTCCHF",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "CHF",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "CHFJPY",
                    Name = "CHFJPY",
                    Accuracy = 3,
                    BaseAssetId = "CHF",
                    QuoteAssetId = "JPY",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "USDCHF",
                    Name = "USDCHF",
                    Accuracy = 3,
                    BaseAssetId = "USD",
                    QuoteAssetId = "CHF",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "EURCHF",
                    Name = "EURCHF",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "CHF",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "BTCJPY",
                    Name = "BTCJPY",
                    Accuracy = 5,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "JPY",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "EURJPY",
                    Name = "EURJPY",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "JPY",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "JPYUSD",
                    Name = "JPYUSD",
                    Accuracy = 3,
                    BaseAssetId = "JPY",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "EURGBP",
                    Name = "EURGBP",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "GBP",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "GBPUSD",
                    Name = "GBPUSD",
                    Accuracy = 3,
                    BaseAssetId = "GBP",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                },
                new AssetPairContract
                {
                    Id = "BLINDR",
                    Name = "BLINDR",
                    Accuracy = 5,
                    BaseAssetId = "BLINDR",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket,
                    ContractSize = 1
                }
            };

            foreach (var pair in assetPairs)
            {
                pair.LegalEntity = "LYKKETEST";
                pair.MatchingEngineMode = MatchingEngineModeContract.MarketMaker;
                pair.StpMultiplierMarkupAsk = 1;
                pair.StpMultiplierMarkupBid = 1;
            }

            var mock = new Mock<IAssetPairsApi>();
            mock.Setup(m => m.List())
                .ReturnsAsync(assetPairs);

            return mock.Object;
        }

        public static void SetOrders(this IMarketMakerMatchingEngine matchingEngine, string marketMakerId, LimitOrder[] ordersToAdd = null, string[] orderIdsToDelete = null, bool deleteAll = false)
        {
            var model = new SetOrderModel
            {
                MarketMakerId = marketMakerId,
                OrdersToAdd = ordersToAdd,
                OrderIdsToDelete = orderIdsToDelete
            };

            if (deleteAll)
            {
                model.DeleteAllBuy = true;
                model.DeleteAllSell = true;
            }
            matchingEngine.SetOrders(model);
        }
    }
}