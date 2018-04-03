using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncFriendlyStackTrace;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using MarginTrading.Backend.Contracts.AccountAssetPair;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Contracts.Client;
using MarginTrading.Backend.Contracts.DataReaderClient;
using MarginTrading.Backend.Contracts.DayOffSettings;
using MarginTrading.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Refit;

namespace MarginTrading.Backend.TestClient
{
    internal static class Program
    {
        private static int _counter;
        
        static async Task Main(string[] args)
        {
            try
            {
                await Run();
            }
            catch (ApiException e)
            {
                var str = e.Content;
                if (str.StartsWith('"'))
                {
                    str = TryDeserializeToString(str);
                }
                
                Console.WriteLine(str);
                Console.WriteLine(e.ToAsyncString());
            }
        }

        private static string TryDeserializeToString(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<string>(str);
            }
            catch
            {
                return str;
            }
        }

        // NOTE: Use Demo instances for tests
        private static async Task Run()
        {
            var services = new ServiceCollection();
            var builder = new ContainerBuilder();
            services.RegisterMtBackendClient("http://localhost:5000", "margintrading", "TestClient");
            services.RegisterMtDataReaderClient("http://localhost:5008", "margintrading", "TestClient");
            builder.Populate(services);
            var container = builder.Build();
            var backendClient = container.Resolve<IMtBackendClient>();

            await backendClient.ScheduleSettings.ListExclusions().Dump();
            var excl = await backendClient.ScheduleSettings.CreateExclusion(new DayOffExclusionInputContract
            {
                AssetPairRegex = "lol",
                Start = DateTime.Now.AddDays(-1),
                End = DateTime.Now.Date,
                IsTradeEnabled = false,
            }).Dump();
            var id = excl.Id;
            var ex = await backendClient.ScheduleSettings.GetExclusion(id).Dump();
            ex.AssetPairRegex = "^btc";
            await backendClient.ScheduleSettings.UpdateExclusion(id, ex).Dump();
            await backendClient.ScheduleSettings.GetExclusion(id).Dump();
            await backendClient.ScheduleSettings.ListCompiledExclusions().Dump();
            await backendClient.ScheduleSettings.DeleteExclusion(id).Dump();
            await backendClient.ScheduleSettings.GetExclusion(id).Dump();

            var s = await backendClient.ScheduleSettings.GetSchedule().Dump();
            s.AssetPairsWithoutDayOff.Add("BTCRABBIT");
            await backendClient.ScheduleSettings.SetSchedule(s).Dump();
            s.AssetPairsWithoutDayOff.Remove("BTCRABBIT");
            await backendClient.ScheduleSettings.SetSchedule(s).Dump();

            var assetPairSettingsInputContract = new AssetPairContract
            {
                Id = "BTCUSD.test",
                BasePairId = "BTCUSD",
                LegalEntity = "LYKKETEST",
                StpMultiplierMarkupBid = 0.9m,
                StpMultiplierMarkupAsk = 1.1m,
                MatchingEngineMode = MatchingEngineModeContract.MarketMaker,
                Name = "BTCUSD.test name",
                Accuracy = 123,
                BaseAssetId = "BTC",
                QuoteAssetId = "USD",
            };

            await backendClient.AssetPairsEdit.Delete("BTCUSD.test").Dump();
            var result = await backendClient.AssetPairsEdit.Insert("BTCUSD.test", assetPairSettingsInputContract).Dump();
            CheckAssetPair(result, assetPairSettingsInputContract);

            assetPairSettingsInputContract.MatchingEngineMode = MatchingEngineModeContract.Stp;
            var result2 = await backendClient.AssetPairsEdit.Update("BTCUSD.test", assetPairSettingsInputContract).Dump();
            CheckAssetPair(result2, assetPairSettingsInputContract);


            var dataReaderClient = container.Resolve<IMtDataReaderClient>();

            var list = await dataReaderClient.AssetPairsRead.List().Dump();
            var ours = list.First(e => e.Id == "BTCUSD.test");
            CheckAssetPair(ours, assetPairSettingsInputContract);

            var get = await dataReaderClient.AssetPairsRead.Get("BTCUSD.test").Dump();
            CheckAssetPair(get, assetPairSettingsInputContract);

            var nonexistentGet = await dataReaderClient.AssetPairsRead.Get("nonexistent").Dump();
            nonexistentGet.RequiredEqualsTo(null, nameof(nonexistentGet));

            var getByMode = await dataReaderClient.AssetPairsRead.Get("LYKKETEST", MatchingEngineModeContract.Stp).Dump();
            var ours2 = getByMode.First(e => e.Id == "BTCUSD.test");
            CheckAssetPair(ours2, assetPairSettingsInputContract);

            var getByOtherMode = await dataReaderClient.AssetPairsRead.Get("LYKKETEST", MatchingEngineModeContract.MarketMaker).Dump();
            getByOtherMode.Count(e => e.Id == "BTCUSD.test").RequiredEqualsTo(0, "getByOtherMode.Count");

            var result3 = await backendClient.AssetPairsEdit.Delete("BTCUSD.test").Dump();
            CheckAssetPair(result3, assetPairSettingsInputContract);

            var nonexistentDelete = await backendClient.AssetPairsEdit.Delete("nonexistent").Dump();
            nonexistentDelete.RequiredEqualsTo(null, nameof(nonexistentDelete));

            #region TradeMonitoring
            var assetSumary = await dataReaderClient.TradeMonitoringRead.AssetSummaryList().Dump();

            var openPositions = await dataReaderClient.TradeMonitoringRead.OpenPositions().Dump();
            string clientId = openPositions.First().ClientId;
            var openPositionsByClient = await dataReaderClient.TradeMonitoringRead.OpenPositionsByClient(clientId).Dump();
            var openPositionsByDate = await dataReaderClient.TradeMonitoringRead.OpenPositionsByDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow).Dump();
            var openPositionsByVolume = await dataReaderClient.TradeMonitoringRead.OpenPositionsByVolume(100).Dump();

            var pendingOrders = await dataReaderClient.TradeMonitoringRead.PendingOrders().Dump();
            var pendingOrdersByClient = await dataReaderClient.TradeMonitoringRead.PendingOrdersByClient(clientId).Dump();
            var pendingOrdersByDate = await dataReaderClient.TradeMonitoringRead.PendingOrdersByDate(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow).Dump();
            var pendingOrdersByVolume = await dataReaderClient.TradeMonitoringRead.PendingOrdersByVolume(100).Dump();

            var orderBooksByInstrument = await dataReaderClient.TradeMonitoringRead.OrderBooksByInstrument("BTCUSD");
            #endregion

            var accountAssetPairs = await dataReaderClient.AccountAssetPairsRead
                .List()
                .Dump();
            var firstAccountAssetPair = accountAssetPairs.First();
            var secondAccountAssetPair = await dataReaderClient.AccountAssetPairsRead
                .Get(firstAccountAssetPair.TradingConditionId, firstAccountAssetPair.BaseAssetId, firstAccountAssetPair.Instrument)
                .Dump();
            firstAccountAssetPair.Should().BeEquivalentTo(secondAccountAssetPair);

            var accountAssetPairsGetByTradingCondition = await dataReaderClient.AccountAssetPairsRead
                .Get(firstAccountAssetPair.TradingConditionId, firstAccountAssetPair.BaseAssetId)
                .Dump();
            foreach (var accountAssetPair in accountAssetPairsGetByTradingCondition)
            {
                var item = accountAssetPairs
                    .Single(x => x.TradingConditionId == accountAssetPair.TradingConditionId
                                && x.BaseAssetId == accountAssetPair.BaseAssetId
                                && x.Instrument == accountAssetPair.Instrument);
                item.Should().BeEquivalentTo(accountAssetPair);
            }

            firstAccountAssetPair.OvernightSwapLong = 0.1m;
            var updatedAccountAssetPair = await backendClient.TradingConditionsEdit.InsertOrUpdateAccountAsset(firstAccountAssetPair)
                .Dump();
            updatedAccountAssetPair.Result.Should().BeEquivalentTo(firstAccountAssetPair);

            var tc = await backendClient.TradingConditionsEdit.InsertOrUpdate(new Contracts.TradingConditions.TradingConditionContract
            {
                Id = "LYKKETEST",
                LegalEntity = "LYKKEVA",
                IsDefault = false,
                Name = "Test Trading Condition",
            }).Dump();
            tc.Result.Id.RequiredEqualsTo("LYKKETEST", "tc.Result.Id");

            var ag = await backendClient.TradingConditionsEdit.InsertOrUpdateAccountGroup(new Contracts.TradingConditions.AccountGroupContract
            {
                BaseAssetId = "BTC",
                TradingConditionId = tc.Result.Id,
                DepositTransferLimit = 0.1m,
                ProfitWithdrawalLimit = 0.2m,
                MarginCall = 0.3m,
                StopOut = 0.4m
            })
            .Dump();
            ag.Result.StopOut.RequiredEqualsTo(0.4m, "ag.Result.StopOut");

            var aa = await backendClient.TradingConditionsEdit.InsertOrUpdateAccountAsset(new AccountAssetPairContract
            {
                Instrument = "TSTLKK",
                BaseAssetId = "BTC",
                TradingConditionId = tc.Result.Id
            })
           .Dump();
            aa.Result.Instrument.RequiredEqualsTo("TSTLKK", "aa.Result.Instrument");

            var ai = await backendClient.TradingConditionsEdit.AssignInstruments(new Contracts.TradingConditions.AssignInstrumentsContract
            {
                BaseAssetId = "BTC",
                TradingConditionId = tc.Result.Id,
                Instruments = new string[] { "TSTLKK" }
            })
            .Dump();

            ai.IsOk.RequiredEqualsTo(true, "ai.IsOk");

            var tclist = await dataReaderClient.TradingConditionsRead.List().Dump();
            await dataReaderClient.TradingConditionsRead.Get(tclist.First().Id).Dump();

            var manualCharge = await backendClient.AccountsBalance.ChargeManually(new Contracts.AccountBalance.AccountChargeManuallyRequest
            {
                ClientId = "232b3b04-7479-44e7-a6b3-ac131d8e6ccd",
                AccountId = "d_f4c745f19c834145bcf2d6b5f1a871f3",
                Amount = 1,
                Reason = "API TEST"
            })
            .Dump();
            Console.WriteLine("Successfuly finished");
        }

        private static void CheckAssetPair(AssetPairContract actual,
            AssetPairContract expected)
        {
            actual.Should().BeEquivalentTo(expected);
        }

        public static T Dump<T>(this T o)
        {
            var str = o is string s ? s : JsonConvert.SerializeObject(o);
            Console.WriteLine("{0}. {1}", ++_counter, str);
            return o;
        }
        
        public static async Task<T> Dump<T>(this Task<T> t)
        {
            return (await t).Dump();
        }
        
        public static async Task Dump(this Task o)
        {
            await o;
            "ok".Dump();
        }
    }
}