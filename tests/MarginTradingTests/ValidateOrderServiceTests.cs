using System;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTradingTests.Helpers;
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

            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = volume
            };

            if (isValid)
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var order = _validateOrderService.ValidateRequestAndGetOrders(request).Result.order;
                        _validateOrderService.MakePreTradeValidation(order, true);

                    });
            }
            else
            {
                var ex = Assert.ThrowsAsync<ValidateOrderException>(
                    async () =>
                    {
                        var order = (await _validateOrderService.ValidateRequestAndGetOrders(request)).order;
                        _validateOrderService.MakePreTradeValidation(order, true);

                    });

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

            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 110, 1.57M);

            var existingShort = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -12, 1.55M);

            var existingOtherAcc = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 49, 1.57M);

            _ordersCache.Positions.Add(existingLong);
            _ordersCache.Positions.Add(existingShort);
            _ordersCache.Positions.Add(existingOtherAcc);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, volume);

            if (isValid)
            {
                Assert.DoesNotThrow(() => _validateOrderService.MakePreTradeValidation(order, true));
            }
            else
            {
                var ex = Assert.Throws<ValidateOrderException>(() =>
                    _validateOrderService.MakePreTradeValidation(order, true));

                Assert.That(ex.RejectReason == OrderRejectReason.InvalidVolume);
            }
        }


        [Test]
        public void Is_Instrument_Ivalid()
        {
            const string instrument = "BADINSRT";
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10
            };

            var ex = Assert.ThrowsAsync<ValidateOrderException>(async () =>
                await _validateOrderService.ValidateRequestAndGetOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }

        [Test]
        public void Is_Account_Ivalid()
        {
            const string accountId = "nosuchaccountId";

            var request = new OrderPlaceRequest
            {
                AccountId = accountId,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = "BTCUSD",
                Type = OrderTypeContract.Market,
                Volume = 10
            };
            
            var ex = Assert.ThrowsAsync<ValidateOrderException>(async () =>
                await _validateOrderService.ValidateRequestAndGetOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidAccount);
        }
        
        [Test]
        public void Is_ValidityDate_Invalid_ForNotMarket()
        {
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = "BTCUSD",
                Type = OrderTypeContract.Limit,
                Price = 1,
                Validity = DateTime.UtcNow.AddSeconds(-1),
                Volume = 10
            };
            
            var ex = Assert.ThrowsAsync<ValidateOrderException>(async () =>
                await _validateOrderService.ValidateRequestAndGetOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.TechnicalError);
        }

        [Test]
        public void Is_No_Quote()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);
            
            var ex = Assert.Throws<QuoteNotFoundException>(() => _validateOrderService.MakePreTradeValidation(order, true));

            Assert.That(ex.InstrumentId == "EURUSD");
        }
        
        [Test]
        public void Is_Not_Enough_Balance()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 150000);

            var ex = Assert.Throws<ValidateOrderException>(() =>
                _validateOrderService.MakePreTradeValidation(order, true));

            Assert.That(ex.RejectReason == OrderRejectReason.NotEnoughBalance);
        }
        
        //TODO: Intruduce order prices validations in MTC-280
//
//        [Test]
//        public void Is_Buy_Order_ExpectedOpenPrice_Invalid()
//        {
//            const string instrument = "EURUSD";
//            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
//            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
//
//            var order = new Position
//            {
//                CreateDate = DateTime.UtcNow,
//                Id = Guid.NewGuid().ToString("N"),
//                AccountId = Accounts[0].Id,
//                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
//                AccountAssetId = Accounts[0].BaseAssetId,
//                AssetPairId = instrument,
//                Volume = 10,
//                ExpectedOpenPrice = 1.58567459M,
//                FillType = OrderFillType.FillOrKill
//            };
//
//            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));
//
//            Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
//            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
//        }
//
//        [Test]
//        public void Is_Sell_Order_ExpectedOpenPrice_Invalid()
//        {
//            const string instrument = "EURUSD";
//            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
//            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
//
//            var order = new Position
//            {
//                CreateDate = DateTime.UtcNow,
//                Id = Guid.NewGuid().ToString("N"),
//                AccountId = Accounts[0].Id,
//                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
//                AccountAssetId = Accounts[0].BaseAssetId,
//                AssetPairId = instrument,
//                Volume = -10,
//                ExpectedOpenPrice = 1.54532567434M,
//                FillType = OrderFillType.FillOrKill
//            };
//
//            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));
//
//            Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
//            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
//        }
//
//        [Test]
//        public void Is_MarketOrder_Buy_TakeProfit_Invalid()
//        {
//            const string instrument = "BTCCHF";
//            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
//            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
//
//            var order = new Position
//            {
//                CreateDate = DateTime.UtcNow,
//                Id = Guid.NewGuid().ToString("N"),
//                AccountId = Accounts[0].Id,
//                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
//                AccountAssetId = Accounts[0].BaseAssetId,
//                AssetPairId = instrument,
//                Volume = 10,
//                TakeProfit = 964.2551256546M,
//                FillType = OrderFillType.FillOrKill
//            };
//
//            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));
//
//            Assert.That(ex.RejectReason == OrderRejectReason.InvalidTakeProfit);
//            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
//            StringAssert.Contains("more", ex.Message);
//        }
//
//        [Test]
//        public void Is_MarketOrder_Sell_TakeProfit_Invalid()
//        {
//            const string instrument = "BTCCHF";
//            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
//            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
//
//            var order = new Position
//            {
//                CreateDate = DateTime.UtcNow,
//                Id = Guid.NewGuid().ToString("N"),
//                AccountId = Accounts[0].Id,
//                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
//                AccountAssetId = Accounts[0].BaseAssetId,
//                AssetPairId = instrument,
//                Volume = -10,
//                TakeProfit = 963.6051356785M,
//                FillType = OrderFillType.FillOrKill
//            };
//
//            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));
//
//            Assert.That(ex.RejectReason == OrderRejectReason.InvalidTakeProfit);
//            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
//            StringAssert.Contains("less", ex.Message);
//        }
//
//
//        [Test]
//        public void Is_MarketOrder_Buy_StopLoss_Invalid()
//        {
//            const string instrument = "BTCCHF";
//            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
//            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
//
//            var order = new Position
//            {
//                CreateDate = DateTime.UtcNow,
//                Id = Guid.NewGuid().ToString("N"),
//                AccountId = Accounts[0].Id,
//                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
//                AccountAssetId = Accounts[0].BaseAssetId,
//                AssetPairId = instrument,
//                Volume = 10,
//                StopLoss = 963.6051245765M,
//                FillType = OrderFillType.FillOrKill
//            };
//
//            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));
//
//            Assert.That(ex.RejectReason == OrderRejectReason.InvalidStoploss);
//            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
//            StringAssert.Contains("less", ex.Message);
//        }
//
//        [Test]
//        public void Is_MarketOrder_Sell_StopLoss_Invalid()
//        {
//            const string instrument = "BTCCHF";
//            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 963.633M, Ask = 964.228M };
//            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
//
//            var order = new Position
//            {
//                CreateDate = DateTime.UtcNow,
//                Id = Guid.NewGuid().ToString("N"),
//                AccountId = Accounts[0].Id,
//                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
//                AccountAssetId = Accounts[0].BaseAssetId,
//                AssetPairId = instrument,
//                Volume = -10,
//                StopLoss = 964.2553256564M,
//                FillType = OrderFillType.FillOrKill
//            };
//
//            var ex = Assert.Throws<ValidateOrderException>(() => _validateOrderService.Validate(order));
//
//            Assert.That(ex.RejectReason == OrderRejectReason.InvalidStoploss);
//            StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
//            StringAssert.Contains("more", ex.Message);
//        }
//
    }
}
