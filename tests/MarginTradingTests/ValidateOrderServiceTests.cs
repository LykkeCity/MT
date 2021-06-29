// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTradingTests.Helpers;
using MarginTradingTests.Services;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class ValidateOrderServiceTests :BaseTests
    {
        
        private IValidateOrderService _validateOrderService;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private OrdersCache _ordersCache;
        private IAssetPairsCache _assetPairsCache;
        private IMatchingEngineBase _me;

        [SetUp]
        public void Setup()
        {
            RegisterDependencies();
            _validateOrderService = Container.Resolve<IValidateOrderService>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _ordersCache = Container.Resolve<OrdersCache>();
            _assetPairsCache = Container.Resolve<IAssetPairsCache>();
            _me = new FakeMatchingEngine(1);
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
                        var order = _validateOrderService.ValidateRequestAndCreateOrders(request).Result.order;
                        _validateOrderService.MakePreTradeValidation(order, true, _me, 0);

                    });
            }
            else
            {
                var ex = Assert.ThrowsAsync<ValidateOrderException>(
                    async () =>
                    {
                        var order = (await _validateOrderService.ValidateRequestAndCreateOrders(request)).order;
                        _validateOrderService.MakePreTradeValidation(order, true, _me, 0);

                    });

                Assert.That(ex.RejectReason ==
                            (volume == 0 ? OrderRejectReason.InvalidVolume : OrderRejectReason.MaxOrderSizeLimit));
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
                Assert.DoesNotThrow(() => _validateOrderService.MakePreTradeValidation(order, true, _me, 0));
            }
            else
            {
                var ex = Assert.Throws<ValidateOrderException>(() =>
                    _validateOrderService.MakePreTradeValidation(order, true, _me, 0));

                Assert.That(ex.RejectReason == OrderRejectReason.MaxPositionLimit);
            }
        }


        [Test]
        public void Is_Not_Existing_Instrument_Invalid()
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
                await _validateOrderService.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Discontinued_Instrument_Invalid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isDiscontinued: true);
            
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
                await _validateOrderService.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Discontinued_Instrument_Invalid_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isDiscontinued: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<ValidateOrderException>(() =>
                _validateOrderService.MakePreTradeValidation(order, true, _me, 0));
            
            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Suspended_Instrument_Invalid_For_Market()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = true
            };

            var ex = Assert.ThrowsAsync<ValidateOrderException>(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }

        [Test]
        public void Is_Suspended_Instrument_Valid_If_Closing_Opened_Positions()
        {
            const string instrument = "EURUSD";

            const bool shouldOpenNewPosition = false;

            SetupAssetPair(instrument, isSuspended: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = shouldOpenNewPosition
            };
            
            Assert.DoesNotThrowAsync(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));
        }
        
        [Test]
        public void Is_Suspended_Instrument_Valid_For_Limit()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Limit,
                Price = 1,
                Volume = 10
            };

            Assert.DoesNotThrowAsync(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));
        }
        
        [Test]
        public void Is_Suspended_Instrument_Invalid_For_Market_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<ValidateOrderException>(() =>
                _validateOrderService.MakePreTradeValidation(order, true, _me, 0));
            
            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Suspended_Instrument_Invalid_For_Limit_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10, price: 1);

            var ex = Assert.Throws<ValidateOrderException>(() =>
                _validateOrderService.MakePreTradeValidation(order, true, _me, 0));
            
            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Frozen_Instrument_Invalid_For_ForceOpen()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isFrozen: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = true
            };

            var ex = Assert.ThrowsAsync<ValidateOrderException>(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Frozen_Instrument_Valid_For_Not_ForceOpen()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isFrozen: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = false
            };

            Assert.DoesNotThrowAsync(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));
        }
        
        [Test]
        public void Is_Frozen_Instrument_Invalid_For_NewPosition_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isFrozen: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<ValidateOrderException>(() =>
                _validateOrderService.MakePreTradeValidation(order, true, _me, 0));
            
            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Frozen_Instrument_Valid_For_PositionClose_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isFrozen: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            Assert.DoesNotThrow(() => _validateOrderService.MakePreTradeValidation(order, false, _me, 0));
        }

        [Test]
        public void Is_Account_Invalid()
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
                await _validateOrderService.ValidateRequestAndCreateOrders(request));

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
                Validity = DateTime.UtcNow.AddDays(-1),
                Volume = 10
            };
            
            var ex = Assert.ThrowsAsync<ValidateOrderException>(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidValidity);
        }
        
        [Test]
        public void Is_ValidityDate_Valid_ForNotMarket()
        {
            var quote = new InstrumentBidAskPair {Instrument = "BTCUSD", Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = OrderDirectionContract.Buy,
                InstrumentId = "BTCUSD",
                Type = OrderTypeContract.Limit,
                Price = 1,
                Validity = DateTime.UtcNow.Date,
                Volume = 10
            };

            Assert.DoesNotThrowAsync(async () =>
                await _validateOrderService.ValidateRequestAndCreateOrders(request));
        }

        [Test]
        public void Is_No_Quote()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);
            
            var ex = Assert.Throws<QuoteNotFoundException>(() => _validateOrderService.MakePreTradeValidation(order, true, _me, 0));

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
                _validateOrderService.MakePreTradeValidation(order, true, _me, 0));

            Assert.That(ex.RejectReason == OrderRejectReason.NotEnoughBalance);
        }
        
        [Test]
        public void Is_Enough_Balance_When_Additional_Margin_Exists()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 150000);

            //account margin = 1000, margin requirement for order = 2355 => additional margin should be > 1355
            
            Assert.DoesNotThrow(() =>
                _validateOrderService.MakePreTradeValidation(order, true, _me, 1356));
        }


        [Test]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Stop, 1, false)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Stop, 1.56, false)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Stop, 2, true)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Stop, 1, true)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Stop, 1.56, false)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Stop, 2, false)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Limit, 2, true)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Limit, 1, true)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Market, 2, true)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Market, 1, true)]
        public void Is_Order_ExpectedOpenPrice_Validated_Correctly(OrderDirectionContract direction, OrderTypeContract orderType, 
            decimal? price, bool isValid)
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = direction,
                InstrumentId = instrument,
                Type = orderType,
                Price = price,
                Volume = 1
            };

            if (isValid)
            {
                Assert.DoesNotThrowAsync(async () =>
                    await _validateOrderService.ValidateRequestAndCreateOrders(request));
            }
            else
            {
                var ex = Assert.ThrowsAsync<ValidateOrderException>(() =>
                    _validateOrderService.ValidateRequestAndCreateOrders(request));

                Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
                StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            }
        }

        [Test]
        [TestCase(OrderDirectionContract.Buy, null, null, null)]
        [TestCase(OrderDirectionContract.Sell, null, null, null)]
        [TestCase(OrderDirectionContract.Buy, 0.1, null, null)]
        [TestCase(OrderDirectionContract.Buy, null, 3, null)]
        [TestCase(OrderDirectionContract.Buy, 0.1, 3, null)]
        [TestCase(OrderDirectionContract.Buy, 3, null, OrderRejectReason.InvalidStoploss)]
        [TestCase(OrderDirectionContract.Buy, null, 0.1, OrderRejectReason.InvalidTakeProfit)]
        [TestCase(OrderDirectionContract.Sell, 3, null, null)]
        [TestCase(OrderDirectionContract.Sell, null, 0.1, null)]
        [TestCase(OrderDirectionContract.Sell, 3, 0.1, null)]
        [TestCase(OrderDirectionContract.Sell, 0.1, null, OrderRejectReason.InvalidStoploss)]
        [TestCase(OrderDirectionContract.Sell, null, 3, OrderRejectReason.InvalidTakeProfit)]
        
        public void Is_RelatedOrder_Validated_Correctly_Against_Base_PendingOrder_On_Create(
            OrderDirectionContract baseDirection,decimal? slPrice, decimal? tpPrice, OrderRejectReason? rejectReason)
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var limitOrderRequest = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = baseDirection,
                InstrumentId = instrument,
                Type = OrderTypeContract.Limit,
                Price = 2,
                StopLoss = slPrice,
                TakeProfit = tpPrice,
                Volume = 1
            };
            
            var stopOrderRequest = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = baseDirection,
                InstrumentId = instrument,
                Type = OrderTypeContract.Stop,
                Price = baseDirection == OrderDirectionContract.Buy ? 2 : 1,
                StopLoss = slPrice,
                TakeProfit = tpPrice,
                Volume = 1
            };

            if (!rejectReason.HasValue)
            {
                Assert.DoesNotThrowAsync(async () =>
                    await _validateOrderService.ValidateRequestAndCreateOrders(limitOrderRequest));
                
                Assert.DoesNotThrowAsync(async () =>
                    await _validateOrderService.ValidateRequestAndCreateOrders(stopOrderRequest));
            }
            else
            {
                var ex1 = Assert.ThrowsAsync<ValidateOrderException>(() =>
                    _validateOrderService.ValidateRequestAndCreateOrders(limitOrderRequest));

                Assert.That(ex1.RejectReason == rejectReason);
                
                var ex2 = Assert.ThrowsAsync<ValidateOrderException>(() =>
                    _validateOrderService.ValidateRequestAndCreateOrders(stopOrderRequest));

                Assert.That(ex2.RejectReason == rejectReason);
            }
        }
        
        [Test]
        [TestCase(OrderDirectionContract.Buy, null, null, null)]
        [TestCase(OrderDirectionContract.Sell, null, null, null)]
        [TestCase(OrderDirectionContract.Buy, 0.1, null, null)]
        [TestCase(OrderDirectionContract.Buy, null, 3, null)]
        [TestCase(OrderDirectionContract.Buy, null, 1.56, null)]
        [TestCase(OrderDirectionContract.Buy, 0.1, 3, null)]
        [TestCase(OrderDirectionContract.Buy, 3, null, OrderRejectReason.InvalidStoploss)]
        [TestCase(OrderDirectionContract.Buy, null, 0.1, OrderRejectReason.InvalidTakeProfit)]
        [TestCase(OrderDirectionContract.Sell, 3, null, null)]
        [TestCase(OrderDirectionContract.Sell, null, 0.1, null)]
        [TestCase(OrderDirectionContract.Sell, null, 1.56, null)]
        [TestCase(OrderDirectionContract.Sell, 3, 0.1, null)]
        [TestCase(OrderDirectionContract.Sell, 0.1, null, OrderRejectReason.InvalidStoploss)]
        [TestCase(OrderDirectionContract.Sell, null, 3, OrderRejectReason.InvalidTakeProfit)]
        
        public void Is_RelatedOrder_Validated_Correctly_Against_Base_MarketOrder_On_Create(
            OrderDirectionContract baseDirection,decimal? slPrice, decimal? tpPrice, OrderRejectReason? rejectReason)
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var orderRequest = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                CorrelationId = Guid.NewGuid().ToString(),
                Direction = baseDirection,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                StopLoss = slPrice,
                TakeProfit = tpPrice,
                Volume = 1
            };
            
            if (!rejectReason.HasValue)
            {
                Assert.DoesNotThrowAsync(async () =>
                    await _validateOrderService.ValidateRequestAndCreateOrders(orderRequest));
            }
            else
            {
                var ex1 = Assert.ThrowsAsync<ValidateOrderException>(() =>
                    _validateOrderService.ValidateRequestAndCreateOrders(orderRequest));

                Assert.That(ex1.RejectReason == rejectReason);
            }
        }

        [Test]
        [TestCase(OrderDirection.Buy, 2.5, true)]
        [TestCase(OrderDirection.Sell, 0.7, true)]
        [TestCase(OrderDirection.Buy, 0.1, false)]
        [TestCase(OrderDirection.Sell, 0.1, false)]
        [TestCase(OrderDirection.Buy, 4, false)]
        [TestCase(OrderDirection.Sell, 4, false)]
        
        public void Is_BaseOrder_Validated_Correctly_Against_Related_On_Change(
            OrderDirection baseDirection, decimal newPrice, bool isValid)
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            Order CreateOrder(OrderType type)
            {
                var order = TestObjectsFactory.CreateNewOrder(type, instrument, Accounts[0],
                    MarginTradingTestsUtils.TradingConditionId,
                    volume: baseDirection == OrderDirection.Buy ? 1 : -1,
                    price: baseDirection == OrderDirection.Buy ? 2 : 1);
                
                _ordersCache.Active.Add(order);
                
                var sl = TestObjectsFactory.CreateNewOrder(OrderType.StopLoss, instrument, 
                    Accounts[0], MarginTradingTestsUtils.TradingConditionId,
                    volume: baseDirection == OrderDirection.Buy ? -1 : 1,
                    price: baseDirection == OrderDirection.Buy ? 0.5M : 3,
                    parentOrderId: order.Id);
                
                var tp = TestObjectsFactory.CreateNewOrder(OrderType.TakeProfit, instrument, 
                    Accounts[0], MarginTradingTestsUtils.TradingConditionId,
                    volume: baseDirection == OrderDirection.Buy ? -1 : 1,
                    price: baseDirection == OrderDirection.Buy ? 3 : 0.5M,
                    parentOrderId: order.Id);
                
                order.AddRelatedOrder(sl);
                order.AddRelatedOrder(tp);
                
                _ordersCache.Inactive.Add(sl);
                _ordersCache.Inactive.Add(tp);

                return order;
            }

            var limitOrder = CreateOrder(OrderType.Limit);

            var stopOrder = CreateOrder(OrderType.Stop);

            if (isValid)
            {
                Assert.DoesNotThrow(() =>
                    _validateOrderService.ValidateOrderPriceChange(limitOrder, newPrice));
                
                Assert.DoesNotThrow(() =>
                    _validateOrderService.ValidateOrderPriceChange(stopOrder, newPrice));
            }
            else
            {
                var ex1 = Assert.Throws<ValidateOrderException>(() =>
                    _validateOrderService.ValidateOrderPriceChange(limitOrder, newPrice));

                Assert.That(ex1.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
                StringAssert.Contains("against related", ex1.Message);
                
                var ex2 = Assert.Throws<ValidateOrderException>(() =>
                    _validateOrderService.ValidateOrderPriceChange(stopOrder, newPrice));

                Assert.That(ex2.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
                StringAssert.Contains("against related", ex1.Message);
            }
        }
        
        private void SetupAssetPair(string id, bool isDiscontinued = false, bool isFrozen = false,
            bool isSuspended = false)
        {
            var pair = _assetPairsCache.GetAssetPairById(id);
            
            _assetPairsCache.AddOrUpdate(
                new AssetPair(pair.Id, pair.Name, pair.BaseAssetId, pair.QuoteAssetId,
                    pair.Accuracy, pair.MarketId, pair.LegalEntity, pair.BaseAssetId, pair.MatchingEngineMode,
                    pair.StpMultiplierMarkupAsk, pair.StpMultiplierMarkupBid,
                    isSuspended, isFrozen, isDiscontinued, pair.AssetType));
            
            var quote = new InstrumentBidAskPair { Instrument = id, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
        }
//
    }
}
