using System;
using System.Collections.Generic;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class ValidateOrderServiceTests :BaseTests
    {
        private IValidateOrderService _validateOrderService;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private OrdersCache _ordersCache;

        [SetUp]
        public void Setup()
        {
            RegisterDependencies();
            _validateOrderService = Container.Resolve<IValidateOrderService>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _ordersCache = Container.Resolve<OrdersCache>();
        }

        [Test]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(10, true)]
        [TestCase(11, false)]
        [TestCase(-1, true)]
        [TestCase(-10, true)]
        [TestCase(-11, false)]
        public void Is_Volume_Ivalid(decimal volume, bool isValid)
        {
            const string instrument = "BTCUSD";

            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = volume,
                FillType = OrderFillType.FillOrKill
            };

            if (isValid)
            {
                Assert.DoesNotThrow(() => _validateOrderService.Validate(order));
            }
            else
            {
                var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

                Assert.That(ex.RejectReason == OrderRejectReason.InvalidVolume);
            }
        }

        [Test]
        [TestCase(2, true)]
        [TestCase(-2, true)]
        [TestCase(3, false)]
        [TestCase(-3, true)]
        [TestCase(10, false)]
        [TestCase(-10, true)]
        public void Is_Summary_Volume_Ivalid(decimal volume, bool isValid)
        {
            const string instrument = "BTCUSD";

            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = 110,
                FillType = OrderFillType.FillOrKill
            };

            var existingShort = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = -12,
                FillType = OrderFillType.FillOrKill
            };

            var existingOtherAcc = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[1].Id,
                ClientId = Accounts[1].ClientId,
                Instrument = instrument,
                Volume = 49,
                FillType = OrderFillType.FillOrKill
            };

            _ordersCache.ActiveOrders.Add(existingLong);
            _ordersCache.ActiveOrders.Add(existingShort);
            _ordersCache.ActiveOrders.Add(existingOtherAcc);

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = volume,
                FillType = OrderFillType.FillOrKill
            };

            if (isValid)
            {
                Assert.DoesNotThrow(() => _validateOrderService.Validate(order));
            }
            else
            {
                var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

                Assert.That(ex.RejectReason == OrderRejectReason.InvalidVolume);
            }
        }

        [Test]
        public void Is_Instrument_Ivalid()
        {
            const string instrument = "BADINSRT";

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = 10,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }

        [Test]
        public void Is_Account_Ivalid()
        {
            const string accountId = "nosuchaccountId";

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = accountId,
                ClientId = Accounts[0].ClientId,
                Instrument = "BTCUSD",
                Volume = 10,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidAccount);
        }

        [Test]
        public void Is_No_Quote()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = "EURUSD",
                Volume = 10,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.NoLiquidity);
        }

        [Test]
        public void Is_Buy_Order_ExpectedOpenPrice_Invalid()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = 10,
                ExpectedOpenPrice = 1.58567459M,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
        }

        [Test]
        public void Is_Sell_Order_ExpectedOpenPrice_Invalid()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = -10,
                ExpectedOpenPrice = 1.54532567434M,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
        }

        [Test]
        public void Is_MarketOrder_Buy_TakeProfit_Invalid()
        {
            const string instrument = "BTCCHF";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = 10,
                TakeProfit = 964.2551256546M,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidTakeProfit);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            StringAssert.Contains("more", ex.Message);
        }

        [Test]
        public void Is_MarketOrder_Sell_TakeProfit_Invalid()
        {
            const string instrument = "BTCCHF";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = -10,
                TakeProfit = 963.6051356785M,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidTakeProfit);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            StringAssert.Contains("less", ex.Message);
        }


        [Test]
        public void Is_MarketOrder_Buy_StopLoss_Invalid()
        {
            const string instrument = "BTCCHF";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = 10,
                StopLoss = 963.6051245765M,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidStoploss);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            StringAssert.Contains("less", ex.Message);
        }

        [Test]
        public void Is_MarketOrder_Sell_StopLoss_Invalid()
        {
            const string instrument = "BTCCHF";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = -10,
                StopLoss = 964.2553256564M,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidStoploss);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            StringAssert.Contains("more", ex.Message);
        }

        [Test]
        public void Is_Not_Enough_Balance()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                Instrument = instrument,
                Volume = 150000,
                FillType = OrderFillType.FillOrKill,
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.NotEnoughBalance);
        }
    }
}
