using System;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Services;
using MarginTrading.Services.Events;
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
        public void Is_Volume_Ivalid(double volume, bool isValid)
        {
            const string instrument = "BTCUSD";

            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55, Ask = 1.57 };
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
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        public void Is_Summary_Volume_Ivalid(double volume, bool isValid)
        {
            const string instrument = "BTCUSD";

            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55, Ask = 1.57 };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = 49,
                FillType = OrderFillType.FillOrKill
            };

            var existingShort = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                Instrument = instrument,
                Volume = -49,
                FillType = OrderFillType.FillOrKill
            };

            _ordersCache.ActiveOrders.Add(existingLong);
            _ordersCache.ActiveOrders.Add(existingShort);

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

            var ex = Assert.Throws<InstrumentNotFoundException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.InstrumentId == instrument);
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


            var ex = Assert.Throws<AccountNotFoundException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.AccountId == accountId);
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

            var ex = Assert.Throws<QuoteNotFoundException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.InstrumentId == order.Instrument);
        }

        [Test]
        public void Is_Buy_Order_ExpectedOpenPrice_Invalid()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55, Ask = 1.57};
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
                ExpectedOpenPrice = 1.58567459,
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
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55, Ask = 1.57 };
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
                ExpectedOpenPrice = 1.54532567434,
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
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633, Ask = 964.228 };
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
                TakeProfit = 964.2551256546,
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
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633, Ask = 964.228 };
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
                TakeProfit = 963.6051356785,
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
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633, Ask = 964.228 };
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
                StopLoss = 963.6051245765,
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
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633, Ask = 964.228 };
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
                StopLoss = 964.2553256564,
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidStoploss);
            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            StringAssert.Contains("more", ex.Message);
        }

        [Test]
        public void Is_Not_Enought_Balance()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55, Ask = 1.57 };
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
                FillType = OrderFillType.FillOrKill
            };

            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));

            Assert.That(ex.RejectReason == OrderRejectReason.NotEnoughBalance);
        }
    }
}
