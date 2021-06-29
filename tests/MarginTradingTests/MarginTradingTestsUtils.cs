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
            return Mock.Of<IAccountsApi>(a => a.List(null, false) == Task.FromResult(list));
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
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30,
                    ShortPosition = true,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 1000000,
                    PositionLimit = 10000000
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURRUB",
                    LeverageInit = 100,
                    LeverageMaintenance = 100,
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 1000000,
                    PositionLimit = 10000000
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCEUR",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30,
                    ShortPosition = true,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCUSD",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30,
                    ShortPosition = true,
                    DealMaxLimit = 10,
                    PositionLimit = 100
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "CHFJPY",
                    LeverageInit = 10,
                    LeverageMaintenance = 15,
                    Delta = 30,
                    ShortPosition = true,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "JPYUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30,
                    ShortPosition = true,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "EURGBP",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30,
                    ShortPosition = true,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "GBPUSD",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30,
                    ShortPosition = true,
                },
                new TradingInstrumentContract
                {
                    TradingConditionId = TradingConditionId,
                    Instrument = "BTCJPY",
                    LeverageInit = 100,
                    LeverageMaintenance = 150,
                    Delta = 30,
                    ShortPosition = true,
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
                    Id = "EURUSD",
                    Name = "EURUSD",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "BTCEUR",
                    Name = "BTCEUR",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "EUR",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "BTCUSD",
                    Name = "BTCUSD",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "BTCCHF",
                    Name = "BTCCHF",
                    Accuracy = 3,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "CHF",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "CHFJPY",
                    Name = "CHFJPY",
                    Accuracy = 3,
                    BaseAssetId = "CHF",
                    QuoteAssetId = "JPY",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "USDCHF",
                    Name = "USDCHF",
                    Accuracy = 3,
                    BaseAssetId = "USD",
                    QuoteAssetId = "CHF",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "EURCHF",
                    Name = "EURCHF",
                    Accuracy = 5,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "CHF",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "BTCJPY",
                    Name = "BTCJPY",
                    Accuracy = 5,
                    BaseAssetId = "BTC",
                    QuoteAssetId = "JPY",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "EURJPY",
                    Name = "EURJPY",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "JPY",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "JPYUSD",
                    Name = "JPYUSD",
                    Accuracy = 3,
                    BaseAssetId = "JPY",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "EURGBP",
                    Name = "EURGBP",
                    Accuracy = 3,
                    BaseAssetId = "EUR",
                    QuoteAssetId = "GBP",
                    MarketId = DefaultMarket
                },
                new AssetPairContract
                {
                    Id = "GBPUSD",
                    Name = "GBPUSD",
                    Accuracy = 3,
                    BaseAssetId = "GBP",
                    QuoteAssetId = "USD",
                    MarketId = DefaultMarket
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