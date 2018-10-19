﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
 using AutoMapper;
 using Lykke.Cqrs;
 using MarginTrading.AccountsManagement.Contracts.Events;
 using MarginTrading.AccountsManagement.Contracts.Models;
 using MarginTrading.Backend.Contracts.Events;
 using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
 using MarginTrading.Backend.Core.Orders;
 using MarginTrading.Backend.Core.Repositories;
 using MarginTrading.Backend.Core.Services;
 using MarginTrading.Backend.Core.Settings;
 using MarginTrading.Backend.Core.Trading;
 using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
 using MarginTrading.Backend.Services.Infrastructure;
 using MarginTrading.Backend.Services.MatchingEngines;
 using MarginTrading.Backend.Services.TradingConditions;
 using MarginTrading.Backend.Services.Workflow;
 using MarginTrading.Common.Services;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.TradingConditions;
 using MarginTradingTests.Helpers;
 using Moq;
 using MoreLinq;
 using NUnit.Framework;


namespace MarginTradingTests
{
    [TestFixture]
    public class TradingEngineTests : BaseTests
    {
        private ITradingEngine _tradingEngine;
        private IMarketMakerMatchingEngine _matchingEngine;
        private ITradingInstrumentsApi _tradingInstruments;
        private const string MarketMaker1Id = "1";
        private IMarginTradingAccount _account;
        private TradingInstrumentsManager _accountAssetsManager;
        private IAccountsCacheService _accountsCacheService;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceChannel;
        //private Mock<IEventChannel<OrderExecutedEventArgs>> _orderExecutedChannelMock;
        private OrdersCache _ordersCache;
        private IFxRateCacheService _fxRateCacheService;
        private IDateService _dateService;
        
        [SetUp]
        public void SetUp()
        {
            RegisterDependencies();

            _account = Accounts[0];

            _bestPriceChannel = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _accountAssetsManager = Container.Resolve<TradingInstrumentsManager>();
            
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _matchingEngine = Container.Resolve<IMarketMakerMatchingEngine>();
            _tradingEngine = Container.Resolve<ITradingEngine>();
            _ordersCache = Container.Resolve<OrdersCache>();
            
//            var orderExecutedChannel = Container.Resolve<IEventChannel<OrderExecutedEventArgs>>();
//            _orderExecutedChannelMock = Mock.Get(orderExecutedChannel);
                
            var quote = new InstrumentBidAskPair { Instrument = "BTCUSD", Bid = 829.69M, Ask = 829.8M };
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(quote));

            _tradingInstruments = Container.Resolve<ITradingInstrumentsApi>();

            var ordersSet1 = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.04M, Volume = 4 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.05M, Volume = 7 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "3", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.1M, Volume = -6 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "4", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.15M, Volume = -8 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet1);

            var contextsNames = Container.Resolve<CqrsContextNamesSettings>();
            var accountsProjection = Container.Resolve<AccountsProjection>();
            var convertService = Container.Resolve<IConvertService>();
            Mock.Get(Container.Resolve<ICqrsEngine>()).Setup(s =>
                s.PublishEvent(It.IsNotNull<PositionClosedEvent>(), contextsNames.TradingEngine))
                .Callback<object, string>(async (ev, s1) =>
                {
                    // simulate the behaviour of account management service 
                    var typedEvent = ev as PositionClosedEvent;
                    var account = _accountsCacheService.Get(typedEvent?.AccountId);
                    account.Balance += typedEvent?.BalanceDelta ?? 0;
                    var accountContract =
                        convertService.Convert<MarginTradingAccount, AccountContract>(account,
                            o => o.ConfigureMap(MemberList.Destination)
                                .ForCtorParam("modificationTimestamp",
                                    p => p.MapFrom(tradingAccount => DateTime.UtcNow)));
                    await accountsProjection.Handle(new AccountChangedEvent(DateTime.UtcNow, "Source", accountContract,
                        AccountChangedEventTypeContract.BalanceUpdated));
                });
            
            
            _fxRateCacheService = Container.Resolve<IFxRateCacheService>();
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURCHF", Ask = 1, Bid = 1 });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1, Bid = 1 });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURUSD", Ask = 1, Bid = 1 });

            _dateService = Container.Resolve<IDateService>();
        }

        #region Market orders

        [Test]
        public void Is_Buy_Order_Rejected_No_Balance()
        {
            var ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
            };

            var quote = new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0082M, Ask = 1.0083M };
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(quote));

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCCHF", _account,
                MarginTradingTestsUtils.TradingConditionId, 4000);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NotEnoughBalance, order.RejectReason);
        }

        [Test]
        public void Is_Sell_Order_Rejected_No_Balance()
        {
            var ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            var quote = new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0082M, Ask = 1.0083M };
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCCHF", _account,
                MarginTradingTestsUtils.TradingConditionId, -4000);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NotEnoughBalance, order.RejectReason);
        }

        [Test]
        public void Is_PartialFill_Buy_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, OrderFillType.PartialFill);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsExecuted(order, new[] {"3", "4"}, 1.1125M);

            ValidatePositionIsOpened(order.Id, 1.04875M, -0.51M);
        }

        [Test]
        public void Is_PartialFill_Sell_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -8, OrderFillType.PartialFill);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsExecuted(order, new[] {"1", "2"}, 1.04875M);
            
            ValidatePositionIsOpened(order.Id, 1.1125M, -0.51M);
        }

        [Test]
        public void Is_Long_Position_Changed()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, 1.1125M);
            
            _ordersCache.Positions.Add(position);
            
            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 8}
            });

            Assert.AreEqual(1.2, position.ClosePrice);
        }

        [Test]
        public void Is_Short_Position_Changed()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -8, 1.04875M);
            
            _ordersCache.Positions.Add(position);
            
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 0.8M, Volume = -8}
            });

            Assert.AreEqual(0.8, position.ClosePrice);
        }

        [Test]
        public void Is_PartialFill_Buy_Partial_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 15, OrderFillType.PartialFill);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsPartiallyExecuted(order, new[] {"3", "4"}, 1M);
        }

        [Test]
        public void Is_PartialFill_Sell_Partial_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -13, OrderFillType.PartialFill);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsPartiallyExecuted(order, new[] {"1", "2"}, 2M);
        }

        [Test]
        public void Is_FillOrKill_Buy_Not_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 16);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsRejected(order, OrderRejectReason.NoLiquidity);
        }

        [Test]
        public void Is_FillOrKill_Sell_Not_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -13);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsRejected(order, OrderRejectReason.NoLiquidity);
        }

        [Test]
        public void Is_FillOrKill_Buy_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 9);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsExecuted(order, new[] {"3", "4"}, 1.11667M);
            
            ValidatePositionIsOpened(order.Id, 1.04778M, -0.62M);
        }

        [Test]
        public void Is_FillOrKill_Sell_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -8);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsExecuted(order, new[] {"1", "2"}, 1.04875M);
            
            ValidatePositionIsOpened(order.Id, 1.1125M, -0.51M);
        }

        [Test]
        public void Is_Long_Position_Closed()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, 1.1125M);
            
            _ordersCache.Positions.Add(position);
            
            Assert.AreEqual(1000, _account.Balance);
            
            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 8 }
            });

            Assert.AreEqual(1.2, position.ClosePrice);

            var order = _tradingEngine.ClosePositionAsync(position.Id, OriginatorType.Investor, "", Guid.NewGuid().ToString()).Result;

            ValidateOrderIsExecuted(order, new[] {"5"}, 1.2M);
            
            ValidatePositionIsClosed(position, 1.2M, 0.7M);

            var account = _accountsCacheService.Get(order.AccountId);
            Assert.AreEqual(1000.7, account.Balance);
        }

        [Test]
        public void Is_Short_Position_Closed()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -8, 1.04875M);
            
            _ordersCache.Positions.Add(position);
            
            var order = _tradingEngine.ClosePositionAsync(position.Id, OriginatorType.Investor, "", Guid.NewGuid().ToString()).Result;

            ValidateOrderIsExecuted(order, new[] {"3", "4"}, 1.1125M);
            
            ValidatePositionIsClosed(position, 1.1125M, -0.51M);

            var account = _accountsCacheService.Get(order.AccountId);
            Assert.AreEqual(999.49, account.Balance);

        }

        [Test]
        public void Is_Order_Limits_Changed()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, price: 1);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1, order.Price);
            Assert.AreEqual(OriginatorType.Investor, order.Originator);
            Assert.AreEqual(null, order.AdditionalInfo);
            
            _tradingEngine.ChangeOrderLimits(order.Id, 0.9M, OriginatorType.OnBehalf, "info", Guid.NewGuid().ToString());

            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(0.9M, order.Price);
            Assert.AreEqual(OriginatorType.OnBehalf, order.Originator);
            Assert.AreEqual("info", order.AdditionalInfo);
        }

        [Test]
        public void Is_Long_Position_Closed_On_TakeProfit()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, 1.1125M);
            
            _ordersCache.Positions.Add(position);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.TakeProfit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -8, parentPositionId: position.Id, price: 1.16M);

            order = _tradingEngine.PlaceOrderAsync(order).GetAwaiter().GetResult();            
            
            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 8}
            });
            
            ValidateOrderIsExecuted(order, new []{"6"}, 1.2M);

            ValidatePositionIsClosed(position, 1.2M, 0.7M, PositionCloseReason.TakeProfit);
            
            var account = _accountsCacheService.Get(order.AccountId);
            Assert.AreEqual(1000.7, account.Balance);
        }

        [Test]
        public void Is_Short_Position_Closed_On_TakeProfit()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -8, 1.04875M);
            
            _ordersCache.Positions.Add(position);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.TakeProfit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, parentPositionId: position.Id, price: 0.8M);

            order = _tradingEngine.PlaceOrderAsync(order).GetAwaiter().GetResult();            
            
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 0.7M, Volume = -8}
            });
            
            ValidateOrderIsExecuted(order, new []{"6"}, 0.7M);

            ValidatePositionIsClosed(position, 0.7M, 2.79M, PositionCloseReason.TakeProfit);
            
            var account = _accountsCacheService.Get(order.AccountId);
            Assert.AreEqual(1002.79, account.Balance);
        }

        [Test]
        public void Is_Long_Position_Closed_On_StopLoss()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 14, 1.12857M);
            
            _ordersCache.Positions.Add(position);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.StopLoss, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -14, parentPositionId: position.Id, price: 0.98M);

            order = _tradingEngine.PlaceOrderAsync(order).GetAwaiter().GetResult();            
            
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 0.9M, Volume = 20}
            }, new[] { "1", "2"});
            
            ValidateOrderIsExecuted(order, new []{"6"}, 0.9M);

            ValidatePositionIsClosed(position, 0.9M, -3.2M, PositionCloseReason.StopLoss);
            
            var account = _accountsCacheService.Get(order.AccountId);
            Assert.AreEqual(996.80002M, account.Balance);
        }

        [Test]
        public void Is_Short_Position_Closed_On_StopLoss()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -11, 1.04636M);
            
            _ordersCache.Positions.Add(position);

            var order = TestObjectsFactory.CreateNewOrder(OrderType.StopLoss, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 11, parentPositionId: position.Id, price: 1.15M);

            order = _tradingEngine.PlaceOrderAsync(order).GetAwaiter().GetResult();            
            
            _matchingEngine.SetOrders("1", new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = -8}
            }, new [] { "3" });

            ValidateOrderIsExecuted(order, new []{"4", "6"}, 1.16364M);
            
            ValidatePositionIsClosed(position, 1.16364M, -1.29M, PositionCloseReason.StopLoss);
            
            var account = _accountsCacheService.Get(order.AccountId);
            Assert.AreEqual(998.70992, account.Balance);
        }

        [Test]
        public async Task Is_Order_Fpl_Correct_With_Commission()
        {
            var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, 1.1125M);
            
            var instrumentContract = new TradingInstrumentContract
            {
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                Instrument = "EURUSD",
                LeverageInit = 100,
                LeverageMaintenance = 150,
                Delta = 30,
                CommissionRate = 0.5M
            };

            Mock.Get(_tradingInstruments).Setup(s => s.List(It.IsAny<string>()))
                .ReturnsAsync(new List<TradingInstrumentContract> {instrumentContract});
            
            await _accountAssetsManager.UpdateTradingInstrumentsCacheAsync();
           
            position.UpdateClosePrice(1.04875M);

            position.GetFpl();

            Assert.AreEqual(-0.51M, Math.Round(position.GetTotalFpl(), 3));
        }

        [Test]
        public void Is_Buy_Cross_Order_Fpl_Correct()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 838.371M, Volume = -15 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.0124M, Bid = 1.0122M });
            
            var position = TestObjectsFactory.CreateOpenedPosition("BTCCHF", _account,
                MarginTradingTestsUtils.TradingConditionId, 1, 838.371M);
            
            _ordersCache.Positions.Add(position);
            
            position.UpdateClosePrice(834.286M);

            position.GetFpl();
            
            var account = _accountsCacheService.Get(position.AccountId);

            Assert.AreEqual(-4.035, Math.Round(position.GetFpl(), 3));
            
            Assert.AreEqual(-4.035, Math.Round(account.GetPnl(), 3));
            
            Assert.AreEqual(position.GetMarginMaintenance(), account.GetUsedMargin());
        }

        [Test]
        public void Is_Sell_Cross_Order_Fpl_Correct()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 838.371M, Volume = -15 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.0124M, Bid = 1.0122M });

            var position = TestObjectsFactory.CreateOpenedPosition("BTCCHF", _account,
                MarginTradingTestsUtils.TradingConditionId, -1, 834.286M);
            
            _ordersCache.Positions.Add(position);
            
            position.UpdateClosePrice(838.371M);

            position.GetFpl();
            
            var account = _accountsCacheService.Get(position.AccountId);

            Assert.AreEqual(-4.035, Math.Round(position.GetFpl(), 3));
            
            Assert.AreEqual(-4.035, Math.Round(account.GetPnl(), 3));
            
            Assert.AreEqual(position.GetMarginMaintenance(), account.GetUsedMargin());
            
        }

        [Test]
        public void Is_Order_Opened_On_Full_Balance()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF",  Bid = 1.0092M, Ask = 1.0095M}));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCCHF", _account,
                MarginTradingTestsUtils.TradingConditionId, 11.0415493502M); //10000 USD (with leverage)
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            ValidateOrderIsExecuted(order, new[] {"5"}, 834.370M);
        }

        //TODO: implement similar test via commands/events
//        [Test]
//        public void Is_Positions_Closed_On_Stopout()
//        {
//            var ordersSet = new []
//            {
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15000 },
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
//            };
//
//            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);
//            
//             _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));
//            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0092M, Ask = 1.0095M }));
//     
//            void CreatePosition(decimal volume)
//            {
//                var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCCHF", _account,
//                    MarginTradingTestsUtils.TradingConditionId, volume);
//
//                _tradingEngine.PlaceOrderAsync(order).GetAwaiter().GetResult();           
//            }
//
//            CreatePosition(1.95M);
//            CreatePosition(1.9M);
//            CreatePosition(1.85M);
//            CreatePosition(1.8M);
//            CreatePosition(1.79M);
//            CreatePosition(1.78M);
//
//            var account = _accountsCacheService.Get(_account.Id);
//
//            Assert.AreEqual(1.62265m, Math.Round(account.GetMarginUsageLevel(), 5));
//            
//            //add new order which will set account to stop out
//            _matchingEngine.SetOrders(MarketMaker1Id,
//                new []{new LimitOrder { CreateDate = DateTime.UtcNow, Id = "7", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 790.286M, Volume = 15000 }
//            }, new[] { "6" });
//
//            Assert.AreEqual(4, account.GetOpenPositionsCount());
//            Assert.AreEqual(380.39099467m, account.GetUsedMargin());
//        }

        [Test]
        public void Is_MarginCall_Reached()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0092M, Ask = 1.0095M }));
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCUSD", Bid = 829.69M, Ask = 829.8M }));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCCHF", _account,
                MarginTradingTestsUtils.TradingConditionId, 11.041549350204821M/*1000 USD (with leverage)*/);

            _tradingEngine.PlaceOrderAsync(order).GetAwaiter().GetResult();   
            
            var account = _accountsCacheService.Get(_account.Id);

            Assert.AreEqual(1.62683m, Math.Round(account.GetMarginUsageLevel(), 5));
            Assert.AreEqual(AccountLevel.None, account.GetAccountLevel()); //no margin call yet

            //add new order which will set account to stop out
            _matchingEngine.SetOrders(MarketMaker1Id,
                new []{new LimitOrder { CreateDate = DateTime.UtcNow, Id = "7", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 808.286M, Volume = 15000 }
            }, new[] { "6" });

            account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(AccountLevel.MarginCall1, account.GetAccountLevel());
        }

        [Test]
        public async Task Check_No_FxRate()
        {
            _bestPriceChannel.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCJPY", Bid = 109.857M, Ask = 130.957M }));

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCJPY", Accounts[1],
                MarginTradingTestsUtils.TradingConditionId, 1);

            order = await _tradingEngine.PlaceOrderAsync(order);

            Assert.AreEqual(OrderStatus.Rejected, order.Status);
        }

        //TODO: implement similar test via commands/events
//        [Test]
//        public void Is_Balance_LessThanZero_On_StopOut_Through_Big_Spread()
//        {
//            var account = Accounts[1];
//            account.Balance = 240000;
//            _accountsCacheService.UpdateAccountBalance(account.Id, account.Balance, DateTime.UtcNow);
//
//            var ordersSet = new[]
//            {
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1097.315M, Volume = 100000 },
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1125.945M, Volume = -100000 },
//            };
//
//            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);
//
//            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCEUR", Accounts[1],
//                MarginTradingTestsUtils.TradingConditionId, 1000);
//            
//            var resultingAccount = _accountsCacheService.Get(order.AccountId);
//            
//            order = _tradingEngine.PlaceOrderAsync(order).Result;
//            
//            ValidateOrderIsExecuted(order, new []{"2"}, 1125.945M);
//            
//            ValidatePositionIsOpened(order.Id, 1097.315M, -28630);
//            
//            Assert.AreEqual(-28630, Math.Round(resultingAccount.GetPnl()));
//
//            ordersSet = new[]
//            {
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1125.039M, Volume = 100000 },
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1126.039M, Volume = -100000 }
//            };
//
//            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);
//
//            var order1 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCEUR", Accounts[1],
//                MarginTradingTestsUtils.TradingConditionId, 1000);
//            
//            order1 = _tradingEngine.PlaceOrderAsync(order1).Result;
//            
//            ValidateOrderIsExecuted(order1, new []{"2"}, 1126.039M);
//            
//            ValidatePositionIsOpened(order1.Id, 1125.039M, -1000);
//            
//            Assert.AreEqual(-1906, Math.Round(resultingAccount.GetPnl()));
//
//            //add orders to create big spread
//            ordersSet = new[]
//            {
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 197.315M, Volume = 100000 },
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 2126.039M, Volume = -100000 }
//            };
//
//            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);
//
//            resultingAccount = _accountsCacheService.Get(order.AccountId);
//            Assert.IsTrue(resultingAccount.Balance < 0);
//        }
        
        //TODO: implement similar test via commands/events
//        [Test]
//        public void Is_Big_Spread_Leads_To_Stopout()
//        {
//            var account = Accounts[1];
//            _accountsCacheService.UpdateAccountBalance(account.Id, 24, DateTime.UtcNow);
//            
//            var ordersSet = new[]
//            {
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "GBPUSD", MarketMakerId = MarketMaker1Id, Price = 2, Volume = 100000 },
//                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "GBPUSD", MarketMakerId = MarketMaker1Id, Price = 6, Volume = -100000 },
//            };
//
//            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);
//
//            _fxRateCacheService.SetQuote(new InstrumentBidAskPair {Instrument = "EURUSD", Ask = 1.5748M, Bid = 1.5748M});
//
//            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "GBPUSD", Accounts[1],
//                MarginTradingTestsUtils.TradingConditionId, 100);
//            
//            var resultingAccount = _accountsCacheService.Get(order.AccountId);
//            
//            order = _tradingEngine.PlaceOrderAsync(order).Result;
//            
//            ValidateOrderIsExecuted(order, new []{"2"}, 6);
//            
//            resultingAccount = _accountsCacheService.Get(order.AccountId);
//            Assert.IsTrue(resultingAccount.Balance < 0);
//        }

        [Test]
        public void Is_Fpl_Margin_Calculated_For_Straight_Pair_Correct()
        {
            var ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "EURGBP", MarketMakerId = MarketMaker1Id, Price = 0.8M, Volume = 100000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "EURGBP", MarketMakerId = MarketMaker1Id, Price = 1M, Volume = -100000 },
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "GBPUSD", Ask = 2M, Bid = 1.5M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURGBP", Ask = 0.8M, Bid = 0.7M });
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURGBP", _account,
                MarginTradingTestsUtils.TradingConditionId, 1000);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            var position = ValidatePositionIsOpened(order.Id, 0.8M, -300);
            
            Assert.AreEqual(10.66666667m, position.GetMarginMaintenance());
            Assert.AreEqual(16.0, position.GetMarginInit());
        }
        
        [Test]
        public void Is_Fpl_Margin_Calculated_For_Reversed_Pair_Correct()
        {
            var ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "CHFJPY", MarketMakerId = MarketMaker1Id, Price = 100.1M, Volume = 100000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "CHFJPY", MarketMakerId = MarketMaker1Id, Price = 100.039M, Volume = -100000 },
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "CHFJPY", Bid = 109.857M, Ask = 130.957M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "EURJPY", Bid = 100.857M, Ask = 110.957M });
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "JPYUSD", Bid = 0.01M, Ask = 0.011M });
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "CHFJPY", _account,
                MarginTradingTestsUtils.TradingConditionId, 1);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            
            var position = ValidatePositionIsOpened(order.Id, 100.1M, 0.001M);

            Assert.AreEqual(0.07340667M, position.GetMarginMaintenance());
            Assert.AreEqual(0.11011M, position.GetMarginInit());
        }

        [Test]
        public void Is_Positions_Liquidated()
        {
            var ordersSet1 = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "7", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.04M, Volume = 100 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "8", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.05M, Volume = -100 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet1, deleteAll: true);
            
            var identityGeneratorMock = new Mock<IIdentityGenerator>();
            identityGeneratorMock.Setup(x => x.GenerateAlphanumericId()).Returns("fake");
            
            var order1 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[3],
                MarginTradingTestsUtils.TradingConditionId, 8);
            var order2 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[4],
                MarginTradingTestsUtils.TradingConditionId, -2);
            
            order1 = _tradingEngine.PlaceOrderAsync(order1).Result;
            order2 = _tradingEngine.PlaceOrderAsync(order2).Result;

            ValidateOrderIsExecuted(order1, new[] {"8",}, 1.05M);
            ValidatePositionIsOpened(order1.Id, 1.04M, -0.08M);
            ValidateOrderIsExecuted(order2, new[] {"7"}, 1.04M);
            ValidatePositionIsOpened(order2.Id, 1.05M, -0.02M);
            
            var orders = _tradingEngine.LiquidatePositionsAsync(new SpecialLiquidationMatchingEngine(2.5M, "Test",
                "test", DateTime.UtcNow), new [] {order1.Id, order2.Id}, "Test").Result;
            
            orders.ForEach(o => ValidateOrderIsExecuted(o, new[] {"test"}, 2.5M));
            Assert.AreEqual(2, orders.Max(x => x.Volume));
            Assert.AreEqual(-8, orders.Min(x => x.Volume));
            Assert.AreEqual(0, _ordersCache.Positions.GetPositionsByInstrument("EURUSD").Count);
        }

        #endregion

        
        #region Pending orders

       
        [Test]
        public void Is_Buy_LimitOrder_Opened()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, price: 1.1M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            //TODO: make pending order margin optional
            //order.UpdatePendingOrderMargin();

            Assert.AreEqual(OrderStatus.Active, order.Status);
            //Assert.AreEqual(0.088, order.GetMarginInit());
            //Assert.AreEqual(0.05866667, order.GetMarginMaintenance());

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 6 }
            });

            var account = _accountsCacheService.Get(order.AccountId);

            ValidateOrderIsExecuted(order, new []{"3","4"}, 1.1125M);
            
            Assert.AreEqual(1, account.GetOpenPositionsCount());
        }

        [Test]
        public void Is_Buy_LimitOrder_Opened_When_Available()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, 8, price: 1.055M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            Assert.IsTrue(account.GetUsedMargin() == 0); //no used margin

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = -6 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.055M, Volume = -6 }
            });


            ValidateOrderIsExecuted(order, new[] {"5", "6"}, 1.05625M);

            ValidatePositionIsOpened(order.Id, 1.04875M, -0.06M);
           
            Assert.AreEqual(1, account.GetOpenPositionsCount()); //position is opened
        }

        [Test]
        public void Is_Sell_LimitOrder_Opened()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -1, price: 1.05M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.Active, order.Status);
            
            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            var account = _accountsCacheService.Get(order.AccountId);

            ValidateOrderIsExecuted(order, new[] {"5"}, 1.06M);

            ValidatePositionIsOpened(order.Id, 1.1M, -0.04M);
            
            Assert.AreEqual(1, account.GetOpenPositionsCount());
        }

        [Test]
        public void Is_Sell_Partial_PendingOrder_Opened_When_Available()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -1, price: 1.07M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            ValidateOrderIsExecuted(order, new []{"6"}, 1.08M);

            ValidatePositionIsOpened(order.Id, 1.1M, -0.02M);
        }

        [Test]
        public void Is_PendingOrder_Canceled()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -1, price: 1.07M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(OrderStatus.Active, order.Status); //is not executed
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _tradingEngine.CancelPendingOrder(order.Id, OriginatorType.Investor, "", Guid.NewGuid().ToString());

            Assert.AreEqual(OrderStatus.Canceled, order.Status);
        }

        [Test]
        public void Is_PendingOrder_NotRejected_On_Trigger()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -10000, price: 1.07M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(OrderStatus.Active, order.Status); //is not executed
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            
            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //is not executed
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status);
        }

        [Test]
        public void Is_PendingOrder_Expires()
        {
            var targetValidity = new DateTime(2100, 1, 1);
            
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Limit, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -1, price: 1.07M, validity: targetValidity);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(OrderStatus.Active, order.Status); //is not executed
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //is not executed
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            
            var ds = Container.Resolve<IDateService>();
            Mock.Get(ds).Setup(s => s.Now()).Returns(targetValidity.AddSeconds(1));

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            Assert.AreEqual(OrderStatus.Expired, order.Status); 
        }    
        
        [Test]
        public void Is_PriceValidated_ForStopOrders_OnChange()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Stop, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, -1, price: 1.07M);
            
            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.AccountId);

            Assert.AreEqual(OrderStatus.Active, order.Status); //is not executed
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            var ex = Assert.Throws<ValidateOrderException>(() =>
                _tradingEngine.ChangeOrderLimits(order.Id, 1.2M, OriginatorType.Investor, "",
                    Guid.NewGuid().ToString()));

            Assert.That(ex.RejectReason == OrderRejectReason.InvalidExpectedOpenPrice);
            StringAssert.Contains("1.05/1.1", ex.Comment);
        }    
        
        #endregion
        
        
        #region Common functions

        [Test]
        [TestCase(new int[0], 1, true)]
        [TestCase(new int[0], -1, true)]
        [TestCase(new[] { 1 }, 1, true)]
        [TestCase(new[] { 1, 2 }, 1, true)]
        [TestCase(new[] { -1, 2 }, 1, true)]
        [TestCase(new[] { -1 }, -1, true)]
        [TestCase(new[] { -1, -2 }, -1, true)]
        [TestCase(new[] { 1, -2 }, -1, true)]
        [TestCase(new[] { -1 }, 1, false)]
        [TestCase(new[] { 1 }, -1, false)]
        [TestCase(new[] { 2 }, -1, false)]
        [TestCase(new[] { -2 }, 1, false)]
        [TestCase(new[] { 2 }, -3, true)]
        [TestCase(new[] { -2 }, 3, true)]
        public void Test_That_Position_Should_Be_Opened_Is_Checked_Correctly(int[] existingVolumes, int newVolume,
            bool shouldOpenPosition)
        {
            foreach (var existingVolume in existingVolumes)
            {
                var position = TestObjectsFactory.CreateOpenedPosition("EURUSD", _account,
                    MarginTradingTestsUtils.TradingConditionId, existingVolume, 1);

                _ordersCache.Positions.Add(position);
            }

            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, newVolume);

            Assert.AreEqual(shouldOpenPosition, _tradingEngine.ShouldOpenNewPosition(order));

            var orderWithForce = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", _account,
                MarginTradingTestsUtils.TradingConditionId, newVolume, forceOpen: true);

            Assert.AreEqual(true, _tradingEngine.ShouldOpenNewPosition(orderWithForce));
        }

        #endregion
        
        
        #region Assert helpers
        
        private void ValidateOrderIsExecuted(Order order, string[] expectedMatchedOrderIds, 
            decimal orderExecutionPrice)
        {
            ValidateMatchedOrders(order, expectedMatchedOrderIds);

            Assert.AreEqual(Math.Abs(order.Volume), order.MatchedOrders.SummaryVolume);
            Assert.AreEqual(orderExecutionPrice, order.ExecutionPrice);
            Assert.AreEqual(OrderStatus.Executed, order.Status);
            Assert.NotNull(order.ExecutionStarted);
            Assert.NotNull(order.Executed);
            Assert.AreEqual(order.Executed, order.LastModified);

            //TODO: create mocks for event chanels
//            _orderExecutedChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(),
//                    It.Is<OrderExecutedEventArgs>(e =>
//                        e.Order.Id == order.Id && e.Order.Status == OrderStatus.ExecutionStarted)),
//                Times.Once());
//            
//            _orderExecutedChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(),
//                    It.Is<OrderExecutedEventArgs>(e =>
//                        e.Order.Id == order.Id && e.Order.Status == OrderStatus.Executed)),
//                Times.Once());
        }
        
        private void ValidateOrderIsPartiallyExecuted(Order order, string[] expectedMatchedOrderIds, 
            decimal volumeLeftToMatch)
        {
            ValidateMatchedOrders(order, expectedMatchedOrderIds);

            Assert.AreEqual(volumeLeftToMatch, Math.Abs(order.Volume) - order.MatchedOrders.SummaryVolume);
            Assert.AreEqual(OrderStatus.ExecutionStarted, order.Status);
            Assert.NotNull(order.ExecutionStarted);
            Assert.GreaterOrEqual(order.LastModified, order.ExecutionStarted);

//            _orderExecutedChannelMock.Verify(x => x.SendEvent(It.IsAny<object>(),
//                    It.Is<OrderExecutedEventArgs>(e =>
//                        e.Order.Id == order.Id && e.Order.Status == OrderStatus.ExecutionStarted)),
//                Times.Once());
        }
        
        private void ValidateOrderIsRejected(Order order, OrderRejectReason reason)
        {
            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(0, order.MatchedOrders.SummaryVolume);
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(reason, order.RejectReason);
        }

        private static void ValidateMatchedOrders(Order order, string[] expectedMatchedOrderIds)
        {
            Assert.AreEqual(expectedMatchedOrderIds.Length, order.MatchedOrders.Count);

            foreach (var id in expectedMatchedOrderIds)
            {
                Assert.AreEqual(1, order.MatchedOrders.Count(item => item.OrderId == id));
            }
        }

        private Position ValidatePositionIsOpened(string positionId, decimal currentPositionClosePrice, decimal positionFpl)
        {
            Assert.IsTrue(_ordersCache.Positions.TryGetPositionById(positionId, out var position), "Position was not opened");
                
            Assert.AreEqual(currentPositionClosePrice, Math.Round(position.ClosePrice, 5));
            Assert.AreEqual(positionFpl, Math.Round(position.GetFpl(), 3));
            Assert.AreEqual(PositionStatus.Active, position.Status);

            return position;
        }

        private void ValidatePositionIsClosed(Position position, decimal closePrice, decimal positionFpl,
            PositionCloseReason closeReason = PositionCloseReason.Close)
        {
            Assert.IsFalse(_ordersCache.Positions.TryGetPositionById(position.Id, out var tmp),
                "Position is still opened");


            Assert.AreEqual(closePrice, position.ClosePrice);
            Assert.AreEqual(positionFpl, Math.Round(position.GetFpl(), 3));
            Assert.AreEqual(PositionStatus.Closed, position.Status);
            Assert.AreEqual(closeReason, position.CloseReason);
        }

        #endregion 
        
    }
}
