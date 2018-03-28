using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncFriendlyStackTrace;
using Autofac;
using Autofac.Extensions.DependencyInjection;
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

        private static async Task Run()
        {
            var services = new ServiceCollection();
            var builder = new ContainerBuilder();
            services.RegisterMtBackendClient("http://localhost:5000", "margintrading", "TestClient");
            services.RegisterMtDataReaderClient("http://localhost:5008", "margintrading", "TestClient");
            builder.Populate(services);
            var container = builder.Build();
            var client = container.Resolve<IMtBackendClient>();

            await client.ScheduleSettings.ListExclusions().Dump();
            var excl = await client.ScheduleSettings.CreateExclusion(new DayOffExclusionInputContract
            {
                AssetPairRegex = "lol",
                Start = DateTime.Now.AddDays(-1),
                End = DateTime.Now.Date,
                IsTradeEnabled = false,
            }).Dump();
            var id = excl.Id;
            var ex = await client.ScheduleSettings.GetExclusion(id).Dump();
            ex.AssetPairRegex = "^btc";
            await client.ScheduleSettings.UpdateExclusion(id, ex).Dump();
            await client.ScheduleSettings.GetExclusion(id).Dump();
            await client.ScheduleSettings.ListCompiledExclusions().Dump();
            await client.ScheduleSettings.DeleteExclusion(id).Dump();
            await client.ScheduleSettings.GetExclusion(id).Dump();

            var s = await client.ScheduleSettings.GetSchedule().Dump();
            s.AssetPairsWithoutDayOff.Add("BTCRABBIT");
            await client.ScheduleSettings.SetSchedule(s).Dump();
            s.AssetPairsWithoutDayOff.Remove("BTCRABBIT");
            await client.ScheduleSettings.SetSchedule(s).Dump();

            var assetPairSettingsInputContract = new AssetPairSettingsInputContract
            {
                BasePairId = "BTCUSD",
                LegalEntity = "LYKKETEST",
                MultiplierMarkupBid = 0.9m,
                MultiplierMarkupAsk = 1.1m,
                MatchingEngineMode = MatchingEngineModeContract.MarketMaker
            };

            await client.AssetPairSettingsEdit.Delete("BTCUSD.test").Dump();
            var result = await client.AssetPairSettingsEdit.Insert("BTCUSD.test", assetPairSettingsInputContract).Dump();
            CheckAssetPairSettings(result, assetPairSettingsInputContract);

            assetPairSettingsInputContract.MatchingEngineMode = MatchingEngineModeContract.Stp;
            var result2 = await client.AssetPairSettingsEdit.Update("BTCUSD.test", assetPairSettingsInputContract).Dump();
            CheckAssetPairSettings(result2, assetPairSettingsInputContract);


            var dataReaderClient = container.Resolve<IMtDataReaderClient>();

            var list = await dataReaderClient.AssetPairSettingsRead.List().Dump();
            var ours = list.First(e => e.AssetPairId == "BTCUSD.test");
            CheckAssetPairSettings(ours, assetPairSettingsInputContract);

            var get = await dataReaderClient.AssetPairSettingsRead.Get("BTCUSD.test").Dump();
            CheckAssetPairSettings(get, assetPairSettingsInputContract);

            var nonexistentGet = await dataReaderClient.AssetPairSettingsRead.Get("nonexistent").Dump();
            nonexistentGet.RequiredEqualsTo(null, nameof(nonexistentGet));

            var getByMode = await dataReaderClient.AssetPairSettingsRead.Get(MatchingEngineModeContract.Stp).Dump();
            var ours2 = getByMode.First(e => e.AssetPairId == "BTCUSD.test");
            CheckAssetPairSettings(ours2, assetPairSettingsInputContract);

            var getByOtherMode = await dataReaderClient.AssetPairSettingsRead.Get(MatchingEngineModeContract.MarketMaker).Dump();
            getByOtherMode.Count(e => e.AssetPairId == "BTCUSD.test").RequiredEqualsTo(0, "getByOtherMode.Count");

            var result3 = await client.AssetPairSettingsEdit.Delete("BTCUSD.test").Dump();
            CheckAssetPairSettings(result3, assetPairSettingsInputContract);

            var nonexistentDelete = await client.AssetPairSettingsEdit.Delete("nonexistent").Dump();
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
            CheckAccountAssetPairs(firstAccountAssetPair, secondAccountAssetPair);

            var accountAssetPairsGetByTradingCondition = await dataReaderClient.AccountAssetPairsRead
                .Get(firstAccountAssetPair.TradingConditionId, firstAccountAssetPair.BaseAssetId)
                .Dump();
            foreach (var accountAssetPair in accountAssetPairsGetByTradingCondition)
            {
                var item = accountAssetPairs
                    .Single(x => x.TradingConditionId == accountAssetPair.TradingConditionId
                                && x.BaseAssetId == accountAssetPair.BaseAssetId
                                && x.Instrument == accountAssetPair.Instrument);
                CheckAccountAssetPairs(item, accountAssetPair);
            }

            firstAccountAssetPair.OvernightSwapLong = 0.1m;
            var updatedAccountAssetPair = await client.TradingConditionsEdit.InsertOrUpdateAccountAsset(firstAccountAssetPair)
                .Dump();
            CheckAccountAssetPairs(updatedAccountAssetPair.Result, firstAccountAssetPair);

            var tc = await client.TradingConditionsEdit.InsertOrUpdate(new Contracts.TradingConditions.TradingConditionContract
            {
                Id = "LYKKETEST",
                IsDefault = false,
                Name = "Test Trading Condition"
            })
                .Dump();
            tc.Result.Id.RequiredEqualsTo("LYKKETEST", "tc.Result.Id");

            var ag = await client.TradingConditionsEdit.InsertOrUpdateAccountGroup(new Contracts.TradingConditions.AccountGroupContract
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

            var aa = await client.TradingConditionsEdit.InsertOrUpdateAccountAsset(new AccountAssetPairContract
            {
                Instrument = "TSTLKK",
                BaseAssetId = "BTC",
                TradingConditionId = tc.Result.Id
            })
           .Dump();
            aa.Result.Instrument.RequiredEqualsTo("TSTLKK", "aa.Result.Instrument");

            var ai = await client.TradingConditionsEdit.AssignInstruments(new Contracts.TradingConditions.AssignInstrumentsContract
            {
                BaseAssetId = "BTC",
                TradingConditionId = tc.Result.Id,
                Instruments = new string[] { "TSTLKK" }
            })
            .Dump();

            ai.IsOk.RequiredEqualsTo(true, "ai.IsOk");

            var tclist = await dataReaderClient.TradingConditionsRead.List().Dump();
            await dataReaderClient.TradingConditionsRead.Get(tclist.First().Id).Dump();

            var manualCharge = await client.AccountsBalance.ChargeManually(new Contracts.AccountBalance.AccountChargeManuallyRequest
            {
                ClientId = "232b3b04-7479-44e7-a6b3-ac131d8e6ccd",
                AccountId = "d_f4c745f19c834145bcf2d6b5f1a871f3",
                Amount = 1,
                Reason = "API TEST"
            })
            .Dump();
            Console.WriteLine("Successfuly finished");
        }

        private static void CheckAssetPairSettings(AssetPairSettingsContract actual,
            AssetPairSettingsInputContract expected)
        {
            actual.AssetPairId.RequiredEqualsTo("BTCUSD.test", nameof(actual.AssetPairId));
            actual.BasePairId.RequiredEqualsTo(expected.BasePairId, nameof(actual.BasePairId));
            actual.LegalEntity.RequiredEqualsTo(expected.LegalEntity, nameof(actual.LegalEntity));
            actual.MultiplierMarkupBid.RequiredEqualsTo(expected.MultiplierMarkupBid,
                nameof(actual.MultiplierMarkupBid));
            actual.MultiplierMarkupAsk.RequiredEqualsTo(expected.MultiplierMarkupAsk,
                nameof(actual.MultiplierMarkupAsk));
            actual.MatchingEngineMode.RequiredEqualsTo(expected.MatchingEngineMode,
                nameof(actual.MatchingEngineMode));
        }
        
        private static void CheckAccountAssetPairs(AccountAssetPairContract actual,
            AccountAssetPairContract expected)
        {
            actual.BaseAssetId.RequiredEqualsTo(expected.BaseAssetId, nameof(actual.BaseAssetId));
            actual.CommissionLong.RequiredEqualsTo(expected.CommissionLong, nameof(actual.CommissionLong));
            actual.CommissionLot.RequiredEqualsTo(expected.CommissionLot, nameof(actual.CommissionLot));
            actual.CommissionShort.RequiredEqualsTo(expected.CommissionShort, nameof(actual.CommissionShort));
            actual.DealLimit.RequiredEqualsTo(expected.DealLimit, nameof(actual.DealLimit));
            actual.DeltaAsk.RequiredEqualsTo(expected.DeltaAsk, nameof(actual.DeltaAsk));
            actual.DeltaBid.RequiredEqualsTo(expected.DeltaBid, nameof(actual.DeltaBid));
            actual.Instrument.RequiredEqualsTo(expected.Instrument, nameof(actual.Instrument));
            actual.LeverageInit.RequiredEqualsTo(expected.LeverageInit, nameof(actual.LeverageInit));
            actual.LeverageMaintenance.RequiredEqualsTo(expected.LeverageMaintenance, nameof(actual.LeverageMaintenance));
            actual.OvernightSwapLong.RequiredEqualsTo(expected.OvernightSwapLong, nameof(actual.OvernightSwapLong));
            actual.OvernightSwapShort.RequiredEqualsTo(expected.OvernightSwapShort, nameof(actual.OvernightSwapShort));
            actual.PositionLimit.RequiredEqualsTo(expected.PositionLimit, nameof(actual.PositionLimit));
            actual.SwapLong.RequiredEqualsTo(expected.SwapLong, nameof(actual.SwapLong));
            actual.SwapShort.RequiredEqualsTo(expected.SwapShort, nameof(actual.SwapShort));
            actual.TradingConditionId.RequiredEqualsTo(expected.TradingConditionId, nameof(actual.TradingConditionId));
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