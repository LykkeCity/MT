// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Autofac;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Services;
using MarginTradingTests.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class FplServiceTests : BaseTests
    {
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private IFxRateCacheService _fxRateCacheService;
        private IAccountsCacheService _accountsCacheService;
        private OrdersCache _ordersCache;
        private IFplService _fplService;
        private IDateService _dateService;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _fxRateCacheService = Container.Resolve<IFxRateCacheService>();
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _ordersCache = Container.Resolve<OrdersCache>();
            _fplService = Container.Resolve<IFplService>();
            _dateService = Container.Resolve<IDateService>();
        }

        [Test]
        public void Is_Fpl_Buy_Correct()
        {
            const string instrument = "BTCUSD";
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument  = instrument, Ask = 800, Bid = 790 }));
            
            var position = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10, 790);

            position.UpdateClosePrice(800);

            Assert.AreEqual(100, position.GetFpl());
        }

        [Test]
        public void Is_Fpl_Sell_Correct()
        {
            const string instrument = "BTCUSD";
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = instrument, Ask = 800, Bid = 790 }));

            var position = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -10, 790);
            
            position.UpdateClosePrice(800);

            Assert.AreEqual(-100, position.GetFpl());
        }

        [Test]
        public void Is_Fpl_Correct_With_Commission()
        {
            const string instrument = "BTCUSD";
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = instrument, Ask = 800, Bid = 790 }));

            var position = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10, 790);

            position.SetCommissionRates(0, 2, 0, 10);
            
            position.UpdateClosePrice(800);

            Assert.AreEqual(80, position.GetTotalFpl());
        }

        [Test]
        public void Is_Fpl_Buy_Cross_Correct()
        {
            const string instrument = "BTCCHF";

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.072030M, Bid = 1.071940M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCUSD", Ask = 1001M, Bid = 1000M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Ask = 901M, Bid = 900M }));
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.072030M, Bid = 1.071940M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "BTCUSD", Ask = 1001M, Bid = 1000M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "BTCCHF", Ask = 901M, Bid = 900M });
            
            var position = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 1000, 935.461M, 0.932888034778M);
            
            position.UpdateClosePrice(935.61M);

            Assert.AreEqual(139m, Math.Round(position.GetFpl(), 3));
        }

        [Test]
        public void Is_Fpl_Sell_Cross_Correct()
        {
            const string instrument = "BTCCHF";

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.072030M, Bid = 1.071940M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCUSD", Ask = 1001M, Bid = 1000M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Ask = 901M, Bid = 900M }));
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.072030M, Bid = 1.071940M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "BTCUSD", Ask = 1001M, Bid = 1000M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "BTCCHF", Ask = 901M, Bid = 900M});

            var position = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -1000, 935.461M, 0.932809716146M);
            
            position.UpdateClosePrice(935.61M);

            Assert.AreEqual(-138.99, Math.Round(position.GetFpl(), 3));
        }

        [Test]
        public void Check_Calculations_As_In_Excel_Document()
        {
            Accounts[0].Balance = 50000;
            _accountsCacheService.UpdateAccountBalance(Accounts[0].Id, Accounts[0].Balance, DateTime.UtcNow);

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Ask = 1.061M, Bid = 1.06M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCEUR", Ask = 1092M, Bid = 1091M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCUSD", Ask = 1001M, Bid = 1000M }));
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1.061M, Bid = 1.06M});
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "BTCEUR", Ask = 1092M, Bid = 1091M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "BTCUSD", Ask = 1001M, Bid = 1000M });
            
            var positions = new List<Position>
            {
                TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 100000, 1.05M),
                
                TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[0],
                    MarginTradingTestsUtils.TradingConditionId, -200000, 1.04M),
                
                TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[0],
                    MarginTradingTestsUtils.TradingConditionId, 50000, 1.061M),
                
                TestObjectsFactory.CreateOpenedPosition("BTCEUR", Accounts[0],
                    MarginTradingTestsUtils.TradingConditionId, 100, 1120, 1.06m)
            };

            foreach (var position in positions)
            {
                _ordersCache.Positions.Add(position);
            }

            positions[0].UpdateClosePrice(1.06M);
            positions[1].UpdateClosePrice(1.061M);
            positions[2].UpdateClosePrice(1.06M);
            positions[3].UpdateClosePrice(1091M);
            
            positions[3].UpdateCloseFxPrice(1.061M);

            var account = Accounts[0];

            Assert.AreEqual(50000, account.Balance);
            Assert.AreEqual(43673.1m, Math.Round(account.GetTotalCapital(), 5));
            Assert.AreEqual(33481.4m, Math.Round(account.GetFreeMargin(), 1));
            Assert.AreEqual(28385.6M, Math.Round(account.GetMarginAvailable(), 1));
            Assert.AreEqual(-6326.9M, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(10191.7M, Math.Round(account.GetUsedMargin(), 1));
            Assert.AreEqual(15287.5M, Math.Round(account.GetMarginInit(), 1));
        }

        [Test]
        public void Check_Order_InitialMargin()
        {
            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1.3M, Bid = 1.2M}));

            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "CHFJPY", Ask = 2.5M, Bid = 2.3M}));
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1.25M, Bid = 1.25M});
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURJPY", Ask = 2.3M, Bid = 2.3M });

            var order1 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 1000);
            
            var order2 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "CHFJPY", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, -100);
            
            Assert.AreEqual(10.4M, _fplService.GetInitMarginForOrder(order1));
            Assert.AreEqual(10M, _fplService.GetInitMarginForOrder(order2));
        }

        [Test]
        public void Check_Position_Margin()
        {
            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1.3M, Bid = 1.2M}));

            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "CHFJPY", Ask = 2.5M, Bid = 2.3M}));
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 9000M, Bid = 9000M});
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURJPY", Ask = 2.3M, Bid = 2.3M });

            var position1 = TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 15000, 1.3M, 1/9000M);
            position1.UpdateClosePrice(1.2M);
            position1.UpdateCloseFxPrice(1/9000M);
            
            var position2 = TestObjectsFactory.CreateOpenedPosition("CHFJPY", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, -100, 2.3M, 1/2.3M);
            position2.UpdateClosePrice(2.5M);
            position2.UpdateCloseFxPrice(1/2.3M);
            
            Assert.AreEqual(0.02M, position1.GetMarginInit());
            Assert.AreEqual(0.01M, position1.GetMarginMaintenance());
            
            Assert.AreEqual(10.87, position2.GetMarginInit());
            Assert.AreEqual(7.25M, position2.GetMarginMaintenance());
        }
        
        [Test]
        public void Check_Position_PnL()
        {
            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1.25M, Bid = 1.15M}));

            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "CHFJPY", Ask = 6.036M, Bid = 1.9M}));
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 9000M, Bid = 9000M});
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURJPY", Ask = 0.83M, Bid = 0.83M });

            var position1 = TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 15000, 1.3M, 0.0001111111111111M);
            position1.UpdateClosePrice(1.15M);
            
            var position2 = TestObjectsFactory.CreateOpenedPosition("CHFJPY", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, -23, 1.96M, 1.204819277108M);
            position2.UpdateClosePrice(6.036M);
            

            Assert.AreEqual(-0.25M, position1.GetFpl());
            Assert.AreEqual(-112.95M, position2.GetFpl());
        }

        [Test]
        public async Task Check_Position_PnL_Multi_Thread()
        {
            _bestPriceConsumer.SendEvent(this,
                new BestPriceChangeEventArgs(
                    new InstrumentBidAskPair {Instrument = "BTCUSD", Ask = 2M, Bid = 1M}));
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1, Bid = 1});
            
            var position1 = TestObjectsFactory.CreateOpenedPosition("BTCUSD", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 1, 2M, 1M);
            
            var updateFxTask = new Action(() => {position1.UpdateCloseFxPrice(2);});
            var getPnLTask = new Action(() => { position1.GetFpl(); });

            var wrong = 0;
            const int attempts = 100000;

            for (int i = 0; i < attempts; i++)
            {
                position1.UpdateClosePrice(1M);
            
                await Task.WhenAll(Task.Factory.StartNew(getPnLTask), Task.Factory.StartNew(updateFxTask));

                var pnl = position1.GetFpl();
                
                if (pnl != -2)
                    wrong++;

                position1.UpdateClosePrice(2M);
                position1.UpdateCloseFxPrice(1);
                Assert.AreEqual(0, position1.GetFpl());
            }

            Assert.AreEqual(0, wrong, $"Number of wrong P&L calculations from {attempts} attempts");
        }
    }
}
