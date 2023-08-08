// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTradingTests.Helpers;
using MarginTradingTests.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class ValidateOrderServiceTests :BaseTests
    {
        
        private IOrderValidator _orderValidator;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private OrdersCache _ordersCache;
        private IAssetPairsCache _assetPairsCache;
        private IMatchingEngineBase _me;
        private ITradingEngine _tradingEngine;

        [SetUp]
        public void Setup()
        {
            RegisterDependencies();
            _orderValidator = Container.Resolve<IOrderValidator>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _ordersCache = Container.Resolve<OrdersCache>();
            _assetPairsCache = Container.Resolve<IAssetPairsCache>();
            _me = new FakeMatchingEngine(1);
            _tradingEngine = Container.Resolve<ITradingEngine>();
        }

        [Test]
        [TestCase(9, true)]
        [TestCase(10, true)]
        [TestCase(11, false)]
        public void ContractSize_Considered_When_ValidateTradeLimits_For_DealMaxLimit(decimal volume, bool isValid)
        {
            const string instrument = "BLINDR";
            const int contractSize = 100;
            
            SetupAssetPair(instrument, contractSize: contractSize);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                // this is the actual volume which comes from donut, already multiplied by contract size
                Volume = volume * contractSize
            };

            if (isValid)
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var order = _orderValidator.ValidateRequestAndCreateOrders(request).Result.order;
                        _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me);
                    });
            }
            else
            {
                var ex = Assert.ThrowsAsync<OrderRejectionException>(
                    async () =>
                    {
                        var order = (await _orderValidator.ValidateRequestAndCreateOrders(request)).order;
                        _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me);

                    });

                Assert.That(ex?.RejectReason == OrderRejectReason.MaxOrderSizeLimit);
            }
        }

        [Test]
        [TestCase(4, true)]
        [TestCase(5, true)]
        [TestCase(6, false)]
        public void ContractSize_Considered_When_ValidateTradeLimits_For_PositionLimit(decimal additionalVolume, bool isValid)
        {
            const string instrument = "BLINDR";
            const int contractSize = 100;
            
            SetupAssetPair(instrument, contractSize: contractSize);
            
            // emulate existing position which already holds 95% of position limit
            var existingLong = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 95 * contractSize, 0.1M);
            _ordersCache.Positions.Add(existingLong);
            
            // request to open new position and potentially overcome the position limit in total
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                // this is the actual volume which comes from donut, already multiplied by contract size
                Volume = additionalVolume * contractSize
            };
            
            if (isValid)
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var order = _orderValidator.ValidateRequestAndCreateOrders(request).Result.order;
                        _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me);
                    });
            }
            else
            {
                var ex = Assert.ThrowsAsync<OrderRejectionException>(
                    async () =>
                    {
                        var order = (await _orderValidator.ValidateRequestAndCreateOrders(request)).order;
                        _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me);

                    });

                Assert.That(ex?.RejectReason == OrderRejectReason.MaxPositionLimit);
            }
        }

        [Test]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(10, true)]
        [TestCase(11, false)]
        [TestCase(-1, true)]
        [TestCase(-10, true)]
        [TestCase(-11, false)]
        public void Is_Volume_Invalid(decimal volume, bool isValid)
        {
            const string instrument = "BTCUSD";

            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
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
                        var order = _orderValidator.ValidateRequestAndCreateOrders(request).Result.order;
                        _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me);
                    });
            }
            else
            {
                var ex = Assert.ThrowsAsync<OrderRejectionException>(
                    async () =>
                    {
                        var order = (await _orderValidator.ValidateRequestAndCreateOrders(request)).order;
                        _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me);

                    });

                Assert.That(ex?.RejectReason ==
                            (volume == 0 ? OrderRejectReason.InvalidVolume : OrderRejectReason.MaxOrderSizeLimit));
            }
        }

        [Test]
        [TestCase(2, true)]
        [TestCase(-2, true)]
        [TestCase(3, false)]
        [TestCase(-3, false)]
        [TestCase(10, false)]
        [TestCase(-10, false)]
        public void Is_Summary_Volume_Invalid(decimal volume, bool isValid)
        {
            const string instrument = "BTCUSD";

            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 1.55M, Ask = 1.57M};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 86, 1.57M);

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
                Assert.DoesNotThrow(() => _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));
            }
            else
            {
                var ex = Assert.Throws<OrderRejectionException>(() =>
                    _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));

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
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10
            };

            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

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
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10
            };

            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Discontinued_Instrument_Invalid_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isDiscontinued: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<OrderRejectionException>(() =>
                _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_TradingDisabled_MarketOrder_Instrument_Invalid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, tradingDisabled: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10
            };

            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InstrumentTradingDisabled);
        }
        
        [Test]
        public void Is_TradingDisabled_LimitOrder_Instrument_Invalid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, tradingDisabled: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Limit,
                Volume = 10
            };

            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InstrumentTradingDisabled);
        }
        
        [Test]
        public void Is_TradingDisabled_Instrument_Invalid_Pre_Trade()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, tradingDisabled: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<OrderRejectionException>(() =>
                _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));

            Assert.That(ex.RejectReason == OrderRejectReason.InstrumentTradingDisabled);
        }
        
        [Test]
        public void Is_Suspended_Instrument_Invalid_For_Market()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = true
            };

            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

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
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = shouldOpenNewPosition
            };
            
            Assert.DoesNotThrowAsync(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));
        }
        
        [Test]
        public void Is_Suspended_Instrument_Valid_For_Limit()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);
            
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Limit,
                Price = 1,
                Volume = 10
            };

            Assert.DoesNotThrowAsync(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));
        }
        
        [Test]
        public void Is_Suspended_Instrument_Invalid_For_Market_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<OrderRejectionException>(() =>
                _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));
            
            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Suspended_Instrument_Invalid_For_Limit_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isSuspended: true);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10, price: 1);

            var ex = Assert.Throws<OrderRejectionException>(() =>
                _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));
            
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
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = true
            };

            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

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
                Direction = OrderDirectionContract.Buy,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = 10,
                ForceOpen = false
            };

            Assert.DoesNotThrowAsync(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));
        }
        
        [Test]
        public void Is_Frozen_Instrument_Invalid_For_NewPosition_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isFrozen: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            var ex = Assert.Throws<OrderRejectionException>(() =>
                _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));
            
            Assert.That(ex.RejectReason == OrderRejectReason.InvalidInstrument);
        }
        
        [Test]
        public void Is_Frozen_Instrument_Valid_For_PositionClose_Pre_Traid()
        {
            const string instrument = "EURUSD";

            SetupAssetPair(instrument, isFrozen: true);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);

            Assert.DoesNotThrow(() => _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, false), _me));
        }

        [Test]
        public void Is_Account_Invalid()
        {
            const string accountId = "nosuchaccountId";

            var request = new OrderPlaceRequest
            {
                AccountId = accountId,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = "BTCUSD",
                Type = OrderTypeContract.Market,
                Volume = 10
            };
            
            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidAccount);
        }
        
        [Test]
        public void Is_ValidityDate_Invalid_ForNotMarket()
        {
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = OrderDirectionContract.Buy,
                InstrumentId = "BTCUSD",
                Type = OrderTypeContract.Limit,
                Price = 1,
                Validity = DateTime.UtcNow.AddDays(-1),
                Volume = 10
            };
            
            var ex = Assert.ThrowsAsync<OrderRejectionException>(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));

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
                Direction = OrderDirectionContract.Buy,
                InstrumentId = "BTCUSD",
                Type = OrderTypeContract.Limit,
                Price = 1,
                Validity = DateTime.UtcNow.Date,
                Volume = 10
            };

            Assert.DoesNotThrowAsync(async () =>
                await _orderValidator.ValidateRequestAndCreateOrders(request));
        }

        [Test]
        public void Is_No_Quote()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);
            
            var ex = Assert.Throws<QuoteNotFoundException>(() => _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));

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

            var ex = Assert.Throws<OrderRejectionException>(() =>
                _orderValidator.PreTradeValidate(OrderFulfillmentPlan.Force(order, true), _me));

            Assert.That(ex.RejectReason == OrderRejectReason.NotEnoughBalance);
        }
        
        [Test]
        public void Is_Enough_Balance_When_Additional_Margin_Exists()
        {
            const string instrument = "EURUSD";
            var quote = new InstrumentBidAskPair { Instrument = instrument, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 151000);

            //account margin = 1000,
            //margin requirement for order = 2355
            //entry cost + exit cost = 1917 => additional margin should be > 3272

            var fulfillmentPlan = OrderFulfillmentPlan.Create(order,
                DumbDataGenerator.GeneratePosition("1", instrument, -1000, 3273, Accounts[0].Id));
            
            Assert.DoesNotThrow(() => _orderValidator.PreTradeValidate(fulfillmentPlan, _me));
        }


        [Test]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Stop, 1, false)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Stop, 1.56, false)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Stop, 2, true)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Stop, 1, true)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Stop, 1.56, false)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Stop, 2, false)]
        [TestCase(OrderDirectionContract.Buy, OrderTypeContract.Limit, 2, false)]
        [TestCase(OrderDirectionContract.Sell, OrderTypeContract.Limit, 1, false)]
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
                Direction = direction,
                InstrumentId = instrument,
                Type = orderType,
                Price = price,
                Volume = 1
            };

            if (isValid)
            {
                Assert.DoesNotThrowAsync(async () =>
                    await _orderValidator.ValidateRequestAndCreateOrders(request));
            }
            else
            {
                var ex = Assert.ThrowsAsync<OrderRejectionException>(() =>
                    _orderValidator.ValidateRequestAndCreateOrders(request));

                Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
                StringAssert.Contains($"{quote.Bid}/{quote.Ask}", ex.Comment);
            }
        }

        [Test]
        [TestCase(OrderDirectionContract.Sell, null, null, null)]
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
                    await _orderValidator.ValidateRequestAndCreateOrders(limitOrderRequest));
                
                Assert.DoesNotThrowAsync(async () =>
                    await _orderValidator.ValidateRequestAndCreateOrders(stopOrderRequest));
            }
            else
            {
                var ex1 = Assert.ThrowsAsync<OrderRejectionException>(() =>
                    _orderValidator.ValidateRequestAndCreateOrders(limitOrderRequest));

                Assert.That(ex1.RejectReason == rejectReason);
                
                var ex2 = Assert.ThrowsAsync<OrderRejectionException>(() =>
                    _orderValidator.ValidateRequestAndCreateOrders(stopOrderRequest));

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
                    await _orderValidator.ValidateRequestAndCreateOrders(orderRequest));
            }
            else
            {
                var ex1 = Assert.ThrowsAsync<OrderRejectionException>(() =>
                    _orderValidator.ValidateRequestAndCreateOrders(orderRequest));

                Assert.That(ex1.RejectReason == rejectReason);
            }
        }

        [Test]
        [TestCase(OrderDirection.Buy, 0.1, false)]
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
                    _orderValidator.ValidateOrderPriceChange(limitOrder, newPrice));
                
                Assert.DoesNotThrow(() =>
                    _orderValidator.ValidateOrderPriceChange(stopOrder, newPrice));
            }
            else
            {
                var ex1 = Assert.Throws<OrderRejectionException>(() =>
                    _orderValidator.ValidateOrderPriceChange(limitOrder, newPrice));

                Assert.That(ex1.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
                StringAssert.Contains("against related", ex1.Message);
                
                var ex2 = Assert.Throws<OrderRejectionException>(() =>
                    _orderValidator.ValidateOrderPriceChange(stopOrder, newPrice));

                Assert.That(ex2.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
                StringAssert.Contains("against related", ex1.Message);
            }
        }

        [Test]
        [TestCase(OrderDirectionContract.Buy, 9, true)]
        [TestCase(OrderDirectionContract.Sell, 11, true)]
        [TestCase(OrderDirectionContract.Buy, 10, false)]
        [TestCase(OrderDirectionContract.Sell, 12, false)]
        
        public void MaxPositionNotionalLimit_Validation_Works_As_Expected_For_ForceOpen(
            OrderDirectionContract direction,
            decimal volume,
            bool isValid)
        {
            const string instrument = "EURUSD";

            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 45, Ask = 55};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 5, It.IsAny<decimal>());

            var existingShort = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -5, It.IsAny<decimal>());

            var existingOtherAcc = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 5, It.IsAny<decimal>());

            _ordersCache.Positions.Add(existingLong);
            _ordersCache.Positions.Add(existingShort);
            _ordersCache.Positions.Add(existingOtherAcc);
            
            ValidateMaxPositionNotionalLimit(instrument, true, direction, volume, isValid);
        }

        [Test]
        [TestCase(OrderDirectionContract.Buy, 16, true)]
        [TestCase(OrderDirectionContract.Sell, 24, true)]
        [TestCase(OrderDirectionContract.Buy, 17, false)]
        [TestCase(OrderDirectionContract.Sell, 25, false)]
        
        public void MaxPositionNotionalLimit_Validation_Works_As_Expected_For_Not_ForceOpen_With_Opposite_Positions(
            OrderDirectionContract direction,
            decimal volume,
            bool isValid)
        {
            const string instrument = "EURUSD";

            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 45, Ask = 55};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 11, It.IsAny<decimal>());

            var existingShort = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -9, It.IsAny<decimal>());

            var existingOtherAcc = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 12, It.IsAny<decimal>());

            _ordersCache.Positions.Add(existingLong);
            _ordersCache.Positions.Add(existingShort);
            _ordersCache.Positions.Add(existingOtherAcc);

            ValidateMaxPositionNotionalLimit(instrument, false, direction, volume, isValid);
        }

        [Test]
        [TestCase(OrderDirectionContract.Buy, 18, true)]
        [TestCase(OrderDirectionContract.Sell, 22, true)]
        [TestCase(OrderDirectionContract.Buy, 19, false)]
        [TestCase(OrderDirectionContract.Sell, 23, false)]
        
        public void MaxPositionNotionalLimit_Validation_Works_As_Expected_For_Not_ForceOpen_Without_Opposite_Positions(
            OrderDirectionContract direction,
            decimal volume,
            bool isValid)
        {
            const string instrument = "EURUSD";

            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 45, Ask = 55};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            ValidateMaxPositionNotionalLimit(instrument, false, direction, volume, isValid);
        }

        [Test]
        [TestCase(21, true)]
        [TestCase(22, false)]
        public void MaxPositionNotionalLimit_Validation_When_Both_Notionals_Before_And_After_Over_Limit(
            decimal volume,
            bool isValid)
        {
            const string instrument = "EURUSD";

            var quote = new InstrumentBidAskPair {Instrument = instrument, Bid = 50, Ask = 60};
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var existingLong = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10, It.IsAny<decimal>());

            var existingShort = TestObjectsFactory.CreateOpenedPosition(instrument, Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -10, It.IsAny<decimal>());

            _ordersCache.Positions.Add(existingLong);
            _ordersCache.Positions.Add(existingShort);

            ValidateMaxPositionNotionalLimit(instrument, false, OrderDirectionContract.Sell, volume, isValid);
        }

        private void ValidateMaxPositionNotionalLimit(
            string instrument,
            bool forceOpen,
            OrderDirectionContract direction,
            decimal volume,
            bool isValid)
        {
            var request = new OrderPlaceRequest
            {
                AccountId = Accounts[0].Id,
                Direction = direction,
                InstrumentId = instrument,
                Type = OrderTypeContract.Market,
                Volume = volume,
                ForceOpen = forceOpen
            };

            if (isValid)
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var order = _orderValidator.ValidateRequestAndCreateOrders(request).Result.order;
                        _orderValidator.PreTradeValidate(_tradingEngine.MatchOnExistingPositions(order), _me);
                    });
            }
            else
            {
                var ex = Assert.ThrowsAsync<OrderRejectionException>(
                    async () =>
                    {
                        var order = (await _orderValidator.ValidateRequestAndCreateOrders(request)).order;
                        _orderValidator.PreTradeValidate(_tradingEngine.MatchOnExistingPositions(order), _me);

                    });

                Assert.That(ex?.RejectReason == OrderRejectReason.MaxPositionNotionalLimit);
            }
        }

        private void SetupAssetPair(string id, bool isDiscontinued = false, bool isFrozen = false,
            bool isSuspended = false, bool tradingDisabled = false, int contractSize = 1)
        {
            var pair = _assetPairsCache.GetAssetPairById(id);

            _assetPairsCache.AddOrUpdate(
                new AssetPair(pair.Id, pair.Name, pair.BaseAssetId, pair.QuoteAssetId,
                    pair.Accuracy, pair.MarketId, pair.LegalEntity, pair.BaseAssetId, pair.MatchingEngineMode,
                    pair.StpMultiplierMarkupAsk, pair.StpMultiplierMarkupBid,
                    isSuspended, isFrozen, isDiscontinued, pair.AssetType, tradingDisabled, contractSize));
            
            var quote = new InstrumentBidAskPair { Instrument = id, Bid = 1.55M, Ask = 1.57M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));
        }
    }
}
