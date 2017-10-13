using MarginTrading.Core;
using NUnit.Framework;
using System;
using Autofac;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Services;
using MarginTrading.Services.MatchingEngines;

namespace MarginTradingTests
{
    [TestFixture]
    public class MatchingEngineRouteTests: BaseTests
    {
        private IAccountsCacheService _accountsCacheService;
        private TradingConditionsManager _tradingConditionsManager;
        private MatchingEngineRoutesManager _matchingEngineRoutesManager;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _tradingConditionsManager = Container.Resolve<TradingConditionsManager>();
            _matchingEngineRoutesManager = Container.Resolve<MatchingEngineRoutesManager>();
            if (_matchingEngineRoutesManager == null)
                throw new Exception("Unable to resolve MatchingEngineRoutesCacheService");

            // Add user accounts
            var account1 = new MarginTradingAccount() {ClientId = "CLIENT001"};
            var account2 = new MarginTradingAccount() {ClientId = "CLIENT002"};
            var account3 = new MarginTradingAccount() {ClientId = "CLIENT003"};
            var account4 = new MarginTradingAccount() {ClientId = "CLIENT004"};
            _accountsCacheService.UpdateAccountsCache(account1.ClientId, new[] {account1});
            _accountsCacheService.UpdateAccountsCache(account2.ClientId, new[] {account2});
            _accountsCacheService.UpdateAccountsCache(account3.ClientId, new[] {account3});
            _accountsCacheService.UpdateAccountsCache(account4.ClientId, new[] {account4});

            // Add trading conditions 
            System.Threading.Tasks.Task.Run(async () =>
            {
                await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(new MarginTradingCondition() { Id = "TCID001", Name= "MarginTradingCondition 1", IsDefault = true });
                await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(new MarginTradingCondition() { Id = "TCID003", Name = "MarginTradingCondition 3", IsDefault = false});
                await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(new MarginTradingCondition() { Id = "TCID004", Name = "MarginTradingCondition 4", IsDefault = false });
                await _tradingConditionsManager.AddOrReplaceTradingConditionAsync(new MarginTradingCondition() { Id = "TCID005", Name = "MarginTradingCondition 5", IsDefault = false });
            }).Wait();
            

            System.Threading.Tasks.Task.Run(async () =>
            {
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "1", Rank = 10, MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "2", Rank = 5, Instrument = "BTCUSD", MatchingEngineId = "ICM" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "3", Rank = 4, TradingConditionId = "TCID001", Instrument = "EURCHF", Type = OrderDirection.Buy, MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "4", Rank = 3, TradingConditionId = "TCID001", ClientId = "CLIENT001", Instrument = "EURCHF", Type = OrderDirection.Buy, MatchingEngineId = "ICM" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "5", Rank = 4, TradingConditionId = "TCID001", ClientId = "CLIENT002", Instrument = "EURCHF", Type = OrderDirection.Buy, MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "6", Rank = 4, Instrument = "EURCHF", Type = OrderDirection.Buy, MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "7", Rank = 4, Instrument = "EURCHF", MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "8", Rank = 5, ClientId = "CLIENT003", Instrument = "EURCHF", MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "9", Rank = 6, TradingConditionId = "TCID003", ClientId = "CLIENT002", Instrument = "EURCHF", Type = OrderDirection.Sell, MatchingEngineId = "ICM" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "10", Rank = 4, TradingConditionId = "TCID004", ClientId = "CLIENT004", Instrument = "EURJPY", MatchingEngineId = "ICM" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "11", Rank = 4, TradingConditionId = "TCID004", Instrument = "EURJPY", Type = OrderDirection.Buy, MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "12", Rank = 4, TradingConditionId = "TCID005", Instrument = "EURUSD", Type = OrderDirection.Buy, MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "13", Rank = 4, TradingConditionId = "TCID005", Instrument = "EURUSD", Type = OrderDirection.Buy, MatchingEngineId = "ICM" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "14", Rank = 9, Instrument = "BTCEUR", MatchingEngineId = "LYKKE" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "15", Rank = 7, Type = OrderDirection.Buy, MatchingEngineId = "ICM", Asset = "EUR" });
                await _matchingEngineRoutesManager.AddOrReplaceRouteAsync(new MatchingEngineRoute() { Id = "16", Rank = 8, Type = OrderDirection.Sell, MatchingEngineId = "ICM", Asset = "EUR" });
            }).Wait();

            /* TABLE PROTOTYPE
                Id	Rank	tradingConditionId	clientId	instrument	type	matchingEngineId	asset

                1	10	    *	                *	        *	        *	    LYKKE
                2	5	    *	                *	        BTCUSD	    *	    ICM
                3	4	    TCID001	            *	        EURCHF	    Buy	    LYKKE
                4	3	    TCID001	            CLIENT001	EURCHF	    Buy	    ICM
                5	4	    TCID001	            CLIENT002	EURCHF	    Buy	    LYKKE
                6	4	    *	                *	        EURCHF	    Buy	    LYKKE
                7	4	    *	                *	        EURCHF	    *	    LYKKE
                8	5	    *	                CLIENT003	EURCHF	    *	    LYKKE
                9	6	    TCID003	            CLIENT002	EURCHF	    Sell	ICM
                10	4	    TCID004	            CLIENT004	EURJPY	    *	    ICM
                11	4	    TCID004	            *	        EURJPY	    Buy	    LYKKE
                12	4	    TCID005	            *	        EURUSD	    Buy	    LYKKE
                13	4	    TCID005	            *	        EURUSD	    Buy	    ICM 
                14	9	    *	                *	        BTCEUR	    *	    LYKKE		
                15	7	    *	                *		                Buy	    ICM	                EUR	
                16	8	    *	                *		                Buy     LYKKE	            EUR	  


             */
        }


        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test01_WildCard_All_But_Instrument()
        {
            // TEST 1 >> CID = CLIENT001, TCID = TCID001, INST=BTCUSD, Buy			
            // Rules that Apply: (ID=1), (ID=2)
            // More specific rule: (ID=2) (BTCUSD)            
            // Result should be ID=2 (Less Generic)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID001", "BTCUSD", OrderDirection.Buy);
            Assert.AreEqual("2", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test02_WildCard_All_But_Instrument()
        {
            // TEST 2 >> CID = CLIENT001, TCID = TCID001, INST=BTCUSD, Sell
            // Rules that Apply: (ID=1), (ID=2)
            // More specific rule: (ID=2) (BTCUSD)            
            // Result should be ID=2 (Less Generic)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID001", "BTCUSD", OrderDirection.Sell);
            Assert.AreEqual("2", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test03_Non_Existing_Instrument()
        {
            // TEST 3 >> CID = CLIENT001, TCID = TCID001, INST=USDBTC, Sell
            // Rules that Apply: (ID=1)            
            // USDBTC has no rule, generic trule applies
            // Result should be ID=1 (Unique Rule)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID001", "BTCJPY", OrderDirection.Sell);
            Assert.AreEqual("1", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test04_Non_Existing_Client()
        {
            // TEST 4 >> CID = CLIENT003, TCID = TCID001, INST=EURCHF, Buy
            // Rules that Apply: (ID=1), (ID=3), (ID=6), (ID=7), (ID=8)
            // Client003 has rule 8 but lower ranked, use higher ranked with more parameters (TCID001,EURCHF,BUY)
            // Result should be ID=3 (Less Generic)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT003", "TCID001", "EURCHF", OrderDirection.Buy);
            Assert.AreEqual("3", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test05_Non_Existing_TradingCondition()
        {
            // TEST 5 >> CID = CLIENT001, TCID = TCID002, INST=EURCHF, Buy
            // Rules that Apply: (ID=1), (ID=6), (ID=7)
            // TCID002 no rule>use generic with more parameters (EURCHF,BUY)
            // Result should be ID=6 (Less Generic)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID002", "EURCHF", OrderDirection.Buy);
            Assert.AreEqual("6", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test06_Non_Existing_TradingCondition_Or_OrderType()
        {
            // TEST 6 >> CID = CLIENT001, TCID = TCID002, INST=EURCHF, Sell
            // Rules that Apply: (ID=1), (ID=7)
            // TCID002 has no rule use generic with more parameters (EURCHF)
            // Result should be ID=7 (Less Generic)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID002", "EURCHF", OrderDirection.Sell);
            Assert.AreEqual("7", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test07_Specific_SameRank()
        {
            // TEST 7 >> CID = CLIENT002, TCID = TCID001, INST=EURCHF, Buy
            // Rules that Apply: (ID=1), (ID=3), (ID=5), (ID=6), (ID=7)
            // There is a specific Rule with all these parameters (Id=5) with same rank than generic rules
            // Result should be ID=5 (Specific)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT002", "TCID001", "EURCHF", OrderDirection.Buy);
            Assert.AreEqual("5", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test08_Specific_HighRank()
        {
            // TEST 8 >>  CID = CLIENT001, TCID = TCID001, INST=EURCHF, Buy
            // Rules that Apply: (ID=1), (ID=3), (ID=4), (ID=6), (ID=7)
            // There is a specific Rule with all these parameters (Id=4) with higher rank than generic rules
            // Result should be ID=4 (High Rank)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID001", "EURCHF", OrderDirection.Buy);
            Assert.AreEqual("4", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test09_Specific_LowRank()
        {
            // TEST 9 >>  CID = CLIENT002, TCID = TCID003, INST=EURJPY, Sell
            // Rules that Apply: (ID=1), (ID=7), (ID=9)
            // There is a specific Rule with all these parameters (Id=9) with lower rank than generic rule
            // Result should be ID=7 (High Rank)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT002", "TCID003", "EURCHF", OrderDirection.Sell);
            Assert.AreEqual("7", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test10_HigherRank()
        {
            // TEST 10 >> CID = CLIENT003, TCID = TCID002, INST=EURCHF, Sell
            // Rules that Apply: (ID=1), (ID=7), (ID=8)
            // More specific rule: (ID=8) (CLIENT003,EURCHF)
            // Rule (ID=7) applies since it is less specific but with higher rank.
            // Result should be ID=7 (High Rank)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT003", "TCID002", "EURCHF", OrderDirection.Sell);
            Assert.AreEqual("7", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test11_MoreSpecific()
        {
            // TEST 11 >> CID = CLIENT003, TCID = TCID002, INST=EURCHF, Buy
            // Rules that Apply: (ID=1), (ID=6), (ID=7), (ID=8)
            // More specific rule: (ID=6) (EURCHF, Buy)            
            // CLIENT003 has rule for EURCHF (ID8) but rules (ID6 and ID7) also apply with higher rank and (ID6) is less generic
            // Result should be ID=6 (High Rank, Less generic)
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT003", "TCID002", "EURCHF", OrderDirection.Buy);
            Assert.AreEqual("6", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]        
        public void Check_Route_Test12_Field_Priority()
        {
            // TEST 12 >> CID = Client004, TCID = TCID004, INST=EURJPY, Buy
            // Rules that Apply: (ID=1), (ID=10), (ID=11)
            // More specific rules: (ID=10) and (ID=11)            
            // Rule (ID=11) takes priority
            // Same rank, same specific level - Rules 10 and 11 apply with same rank and specific level, priority falls to ID=10 (client over type).
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT004", "TCID004", "EURJPY", OrderDirection.Buy);
            Assert.AreEqual("10", res.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        [Category("Error")]
        public void Check_Route_Test13_Error()
        {
            // TEST 13 >> CID = Client005, TCID = TCID005, INST=EURUSD, Buy
            // Rules that Apply: (ID=1), (ID=12), (ID=13)
            // More specific rule: (ID=12) and (ID=13)            
            // Same rank, same specific level, same priority - not defined            
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT005", "TCID005", "EURUSD", OrderDirection.Buy);
            Assert.IsNull(res?.Id);
        }

        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test14_Asset_DoesntApply()
        {
            // TEST 14 >> CID = Client006, TCID = TCID006, INST=BTCEUR, Sell
            // Rules that Apply: (ID=1), (ID=14)
            // More specific rules: (ID=14)            
            // Rule (ID=14) Max Rank
            // There is asset Rule (ID=15) but doesn't apply to SELL
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT006", "TCID006", "BTCEUR", OrderDirection.Sell);
            Assert.AreEqual("14", res.Id);
        }
        [Test]
        [Category("MatchingEngineRouter")]
        public void Check_Route_Test15_Asset_MaxRanked()
        {
            // TEST 15 >> CID = Client006, TCID = TCID006, INST=BTCEUR, Buy
            // Rules that Apply: (ID=1), (ID=14), (ID=15)
            // More specific rules: (ID=14)            
            // Rule (ID=15) Max Rank
            // Asset Rule (ID=15) has higher rank
            var res = _matchingEngineRoutesManager.FindRoute("CLIENT006", "TCID006", "BTCEUR", OrderDirection.Buy);
            Assert.AreEqual("15", res.Id);
        }




        [Test]
        [Category("MatchingEngineRouterManager")]
        public void CreateRoute()
        {
            // TEST 1 >> CID = CLIENT001, TCID = TCID001, INST=BTCUSD, Buy			
            // Rules that Apply: (ID=1), (ID=2)
            // More specific rule: (ID=2) (BTCUSD)            
            // Result should be ID=2 (Less Generic)
            //var res = _matchingEngineRoutesManager.FindRoute("CLIENT001", "TCID001", "BTCUSD", OrderDirection.Buy);
            //Assert.AreEqual("2", res.Id);
        }
    }
}
