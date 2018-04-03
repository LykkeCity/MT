﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Notifications;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Contract.BackendContracts;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class TradingEngineTests : BaseTests
    {
        private ITradingEngine _tradingEngine;
        private IMarketMakerMatchingEngine _matchingEngine;
        private IAccountAssetPairsRepository _accountAssetPairsRepository;
        private const string MarketMaker1Id = "1";
        private string _acount1Id;
        private string _client1Id;
        private AccountAssetsManager _accountAssetsManager;
        private AccountManager _accountManager;
        private IAccountsCacheService _accountsCacheService;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private Mock<IClientNotifyService> _clientNotifyServiceMock;
        private Mock<IAppNotifications> _appNotificationsMock;
        private Mock<IEmailService> _emailServiceMock;

        [SetUp]
        public void SetUp()
        {
            RegisterDependencies();

            _acount1Id = Accounts[0].Id;
            _client1Id = Accounts[0].ClientId;

            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _accountAssetsManager = Container.Resolve<AccountAssetsManager>();
            _accountManager = Container.Resolve<AccountManager>();
            
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _matchingEngine = Container.Resolve<IMarketMakerMatchingEngine>();
            _tradingEngine = Container.Resolve<ITradingEngine>();

            var clientNotifyService = Container.Resolve<IClientNotifyService>();
            var appNotifications = Container.Resolve<IAppNotifications>();
            var emailService = Container.Resolve<IEmailService>();
            _clientNotifyServiceMock = Mock.Get(clientNotifyService);
            _appNotificationsMock = Mock.Get(appNotifications);
            _emailServiceMock = Mock.Get(emailService);

            var quote = new InstrumentBidAskPair { Instrument = "BTCUSD", Bid = 829.69M, Ask = 829.8M };
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            _accountAssetPairsRepository = Container.Resolve<IAccountAssetPairsRepository>();

            var ordersSet1 = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.04M, Volume = 4 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.05M, Volume = 7 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "3", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.1M, Volume = -6 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "4", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.15M, Volume = -8 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet1);
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
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = 4000,
                FillType = OrderFillType.FillOrKill
            };

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
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(quote));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = -4000,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NotEnoughBalance, order.RejectReason);
        }

        [Test]
        public void Is_PartialFill_Buy_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.1125, order.OpenPrice);
            Assert.AreEqual(1.04875, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_PartialFill_Sell_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_PartialFill_Buy_ClosePrice_Changed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.1125, order.OpenPrice);
            Assert.AreEqual(1.04875, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);

            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 8}
            });

            Assert.AreEqual(1.2, order.ClosePrice);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)), Times.AtLeastOnce());
        }

        [Test]
        public void Is_PartialFill_Sell_ClosePrice_Changed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 0.8M, Volume = -8}
            });

            Assert.AreEqual(0.8, order.ClosePrice);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)), Times.AtLeastOnce());
        }

        [Test]
        public void Is_PartialFill_Buy_Partial_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 15,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(1, order.Volume - order.GetMatchedVolume());
            Assert.AreEqual(1.12857, order.OpenPrice);
            Assert.AreEqual(1.04636, order.ClosePrice);
            Assert.AreEqual(-1.15094, Math.Round(order.GetFpl(), 5));
            Assert.AreEqual(OrderStatus.Active, order.Status);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_PartialFill_Sell_Partial_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -13,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(2, Math.Abs(order.Volume) - order.GetMatchedVolume());
            Assert.AreEqual(1.04636, order.OpenPrice);
            Assert.AreEqual(1.12273, order.ClosePrice);
            Assert.AreEqual(-0.84007, Math.Round(order.GetFpl(), 5));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_FillOrKill_Buy_Not_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 16,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(0, order.GetMatchedVolume());
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NoLiquidity, order.RejectReason);
        }

        [Test]
        public void Is_FillOrKill_Sell_Not_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -13,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(0, order.GetMatchedVolume());
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NoLiquidity, order.RejectReason);
        }

        [Test]
        public void Is_FillOrKill_Buy_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 9,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.11667, order.OpenPrice);
            Assert.AreEqual(1.04778, order.ClosePrice);
            Assert.AreEqual(-0.62001, Math.Round(order.GetFpl(), 5));
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_FillOrKill_Sell_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_PartialFill_Buy_Order_Closed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                FillType = OrderFillType.PartialFill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.1125, order.OpenPrice);
            Assert.AreEqual(1.04875, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);

            Assert.AreEqual(1000, account.Balance);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
            
            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 8 }
            });

            Assert.AreEqual(1.2, order.ClosePrice);

            order = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;

            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(OrderCloseReason.Close, order.CloseReason);
            Assert.AreEqual(1, order.MatchedCloseOrders.Count);
            Assert.AreEqual(0.7, order.GetTotalFpl());
            Assert.AreEqual(1000.7, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(),
                    It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_PartialFill_Sell_Order_Closed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.PartialFill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            order = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;

            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(OrderCloseReason.Close, order.CloseReason);
            Assert.AreEqual(2, order.MatchedCloseOrders.Count);
            Assert.AreEqual(-0.51, order.GetTotalFpl());
            Assert.AreEqual(999.49, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_FillOrKill_Buy_Order_Closed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 9,
                FillType = OrderFillType.FillOrKill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.11667, order.OpenPrice);
            Assert.AreEqual(1.04778, order.ClosePrice);
            Assert.AreEqual(-0.62001, Math.Round(order.GetFpl(), 5));
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            order = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;

            Assert.AreEqual(2, order.MatchedCloseOrders.Count);
            Assert.True(order.MatchedCloseOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedCloseOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.11667, order.OpenPrice);
            Assert.AreEqual(1.04778, order.ClosePrice);
            Assert.AreEqual(-0.62001, Math.Round(order.GetFpl(), order.AssetAccuracy));
            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(OrderCloseReason.Close, order.CloseReason);
            Assert.AreEqual(999.37999, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_FillOrKill_Sell_Order_Closed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.FillOrKill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            order = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;

            Assert.AreEqual(2, order.MatchedCloseOrders.Count);
            Assert.True(order.MatchedCloseOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedCloseOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.AreEqual(-0.51, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(OrderCloseReason.Close, order.CloseReason);
            Assert.AreEqual(999.49, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_FillOrKill_Buy_Order_Closed_When_FullyMatched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 9,
                FillType = OrderFillType.FillOrKill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            //remove limit order, so when we are closing order we can't fully match it
            _matchingEngine.SetOrders("1", new LimitOrder[] {}, new [] { "1" });

            order = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;
            Assert.AreEqual(OrderStatus.Closing, order.Status); //order is in closing state
            Assert.AreEqual(7, order.GetMatchedCloseVolume()); //partially matched

            //add new limit order, so active order waiting for close can be matched
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.04M, Volume = 1}
            });

            Assert.AreEqual(8, order.GetMatchedCloseVolume());

            //adding another order which should fill the active order
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.04M, Volume = 1}
            });

            //order should now be fully matched and closed
            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedCloseVolume());
            Assert.AreEqual(-0.60003, order.GetTotalFpl());
            Assert.AreEqual(999.39997, account.Balance);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_FillOrKill_Sell_Order_Closed_When_FullyMatched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            //remove limit order, so when we are closing order we can't fully match it
            _matchingEngine.SetOrders("1", new LimitOrder[] { }, new [] { "4" });

            order = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;
            Assert.AreEqual(OrderStatus.Closing, order.Status); //order is in closing state
            Assert.AreEqual(6, order.GetMatchedCloseVolume()); //partially matched

            //add new limit order, so active order waiting for close can be matched
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.1M, Volume = -1}
            });

            Assert.AreEqual(7, order.GetMatchedCloseVolume());

            //adding another order which should fill the active order
            _matchingEngine.SetOrders("1", new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.1M, Volume = -1}
            });

            ////order should fully matched and closed
            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedCloseVolume());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Order_Limits_Changed()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            Assert.IsNull(order.StopLoss);
            Assert.IsNull(order.TakeProfit);
            Assert.AreEqual(OrderStatus.Active, order.Status);

            _tradingEngine.ChangeOrderLimits(order.Id, 0.99M, 1.3M, null);

            Assert.AreEqual(0.99, order.StopLoss);
            Assert.AreEqual(1.3, order.TakeProfit);
        }

        [Test]
        public void Is_Buy_Order_Closed_On_TakeProfit()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                TakeProfit = 1.16M,
                FillType = OrderFillType.PartialFill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.1125, order.OpenPrice);
            Assert.AreEqual(1.04875, order.ClosePrice);
            Assert.IsTrue(order.ClosePrice < order.TakeProfit);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 8}
            });

            Assert.AreEqual(OrderStatus.Closed, order.Status); //order should be closed on TakeProfit
            Assert.AreEqual(OrderCloseReason.TakeProfit, order.CloseReason);
            Assert.AreEqual(0.7, order.GetTotalFpl());
            Assert.AreEqual(1000.7, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Sell_Order_Closed_On_TakeProfit()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                TakeProfit = 0.8M,
                FillType = OrderFillType.PartialFill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04875, order.OpenPrice);
            Assert.AreEqual(1.1125, order.ClosePrice);
            Assert.IsTrue(order.ClosePrice > order.TakeProfit); //no takeprofit
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 0.7M, Volume = -8}
            });

            Assert.AreEqual(OrderStatus.Closed, order.Status); //order should be closed on TakeProfit
            Assert.AreEqual(OrderCloseReason.TakeProfit, order.CloseReason);
            Assert.AreEqual(2.79, order.GetTotalFpl());
            Assert.AreEqual(1002.79, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Buy_Order_Closed_On_StopLoss()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id, 
                Instrument = "EURUSD",
                Volume = 14,
                StopLoss = 0.98M,
                FillType = OrderFillType.FillOrKill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.12857, order.OpenPrice);
            Assert.AreEqual(1.04636, order.ClosePrice);
            Assert.IsTrue(order.ClosePrice > order.StopLoss); //no stoploss
            Assert.IsNull(order.StartClosingDate);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders("1", new[]
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 0.9M, Volume = 20}
            }, new[] { "1"});

            Assert.AreEqual(OrderStatus.Closed, order.Status); //order is closed now on StopLoss
            Assert.AreEqual(OrderCloseReason.StopLoss, order.CloseReason);
            Assert.AreEqual(-2.14998, order.GetTotalFpl());
            Assert.AreEqual(997.85002, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Sell_Order_Closed_On_StopLoss()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -11,
                StopLoss = 1.15M,
                FillType = OrderFillType.FillOrKill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1.04636, order.OpenPrice);
            Assert.AreEqual(1.12273, order.ClosePrice);
            Assert.IsTrue(order.ClosePrice < order.StopLoss); // no stoploss
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders("1", new []
            {
               new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = -8}
            }, new [] { "3" });

            Assert.AreEqual(OrderStatus.Closed, order.Status); //order should be closed on StopLoss
            Assert.AreEqual(OrderCloseReason.StopLoss, order.CloseReason);
            Assert.AreEqual(-1.29008, order.GetTotalFpl());
            Assert.AreEqual(998.70992, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public async Task Is_Order_Fpl_Correct_With_Commission()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                FillType = OrderFillType.PartialFill
            };

            _accountAssetPairsRepository.AddOrReplaceAsync(new AccountAssetPair
            {
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                BaseAssetId = "USD",
                Instrument = "EURUSD",
                LeverageInit = 100,
                LeverageMaintenance = 150,
                DeltaAsk = 30,
                DeltaBid = 30,
                CommissionShort = 0.5M,
                CommissionLong = 1,
                CommissionLot = 8
            }).Wait();

            await _accountAssetsManager.UpdateAccountAssetsCache();
           
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(1.1125, order.OpenPrice);
            Assert.AreEqual(1.04875, order.ClosePrice);
            Assert.AreEqual(-1.51, Math.Round(order.GetTotalFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.IsAny<OrderHistoryBackendContract>()), Times.Once());
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

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0122M, Ask = 1.0124M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = 1,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(838.371, order.OpenPrice);
            Assert.AreEqual(834.286, order.ClosePrice);
            Assert.AreEqual(828.103, order.GetOpenCrossPrice());
            Assert.AreEqual(824.068, order.GetCloseCrossPrice());
            Assert.AreEqual(-4.035, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);
            Assert.AreEqual(-4.035, Math.Round(account.GetPnl(), 3));
            Assert.AreEqual(order.GetMarginMaintenance(), account.GetUsedMargin());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Buy_Cross_Order_Fpl_Correct1()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 838.371M, Volume = -15 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0122M, Ask = 1.0124M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = 1,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(838.371, order.OpenPrice);
            Assert.AreEqual(834.286, order.ClosePrice);
            Assert.AreEqual(828.103, order.GetOpenCrossPrice());
            Assert.AreEqual(824.068, order.GetCloseCrossPrice());
            Assert.AreEqual(-4.035, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);

            Assert.AreEqual(-4.035, Math.Round(account.GetPnl(), 3));
            Assert.AreEqual(order.GetMarginMaintenance(), account.GetUsedMargin());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
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

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0122M, Ask = 1.0124M}));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = -1,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(834.286, order.OpenPrice);
            Assert.AreEqual(838.371, order.ClosePrice);
            Assert.AreEqual(824.068, order.GetOpenCrossPrice());
            Assert.AreEqual(828.103, order.GetCloseCrossPrice());
            Assert.AreEqual(-4.035, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);

            Assert.AreEqual(-4.035, Math.Round(account.GetPnl(), 3));
            Assert.AreEqual(order.GetMarginMaintenance(), account.GetUsedMargin());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Sell_Cross_Order_Fpl_Correct1()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 838.371M, Volume = -15 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0122M, Ask = 1.0124M }));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = -1,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(834.286, order.OpenPrice);
            Assert.AreEqual(838.371, order.ClosePrice);
            Assert.AreEqual(824.068, order.GetOpenCrossPrice());
            Assert.AreEqual(828.103, order.GetCloseCrossPrice());
            Assert.AreEqual(-4.035, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsNull(order.StartClosingDate);

            Assert.AreEqual(-4.035, Math.Round(account.GetPnl(), 3));
            Assert.AreEqual(order.GetMarginMaintenance(), account.GetUsedMargin());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
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

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF",  Bid = 1.0092M, Ask = 1.0095M}));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = 11.14644406903176M,  //10000 USD (with leverage)
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            Assert.AreEqual(OrderStatus.Active, order.Status);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Orders_Closed_On_Stopout()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0092M, Ask = 1.0095M }));

            
            //3 orders = 10000 USD (with leverage)

            Order CreateOrder(decimal volume)
            {
                return new Order
                {
                    CreateDate = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString("N"),
                    AccountId = _acount1Id,
                    ClientId = _client1Id,
                    Instrument = "BTCCHF",
                    Volume = volume,
                    FillType = OrderFillType.FillOrKill
                };
            };

            var order1 = CreateOrder(1.95M);
            var order2 = CreateOrder(1.9M);
            var order3 = CreateOrder(1.85M);
            var order4 = CreateOrder(1.8M);
            var order5 = CreateOrder(1.79M);
            var order6 = CreateOrder(1.78M);

            order1 = _tradingEngine.PlaceOrderAsync(order1).Result;
            order2 = _tradingEngine.PlaceOrderAsync(order2).Result;
            order3 = _tradingEngine.PlaceOrderAsync(order3).Result;
            order4 = _tradingEngine.PlaceOrderAsync(order4).Result;
            order5 = _tradingEngine.PlaceOrderAsync(order5).Result;
            order6 = _tradingEngine.PlaceOrderAsync(order6).Result;
            
            var account = _accountsCacheService.Get(_client1Id, _acount1Id);

            Assert.AreEqual(OrderStatus.Active, order1.Status);
            Assert.AreEqual(OrderStatus.Active, order2.Status);
            Assert.AreEqual(OrderStatus.Active, order3.Status);
            Assert.AreEqual(OrderStatus.Active, order4.Status);
            Assert.AreEqual(OrderStatus.Active, order5.Status);
            Assert.AreEqual(OrderStatus.Active, order6.Status);
            
            Assert.AreEqual(1.63808m, Math.Round(account.GetMarginUsageLevel(), 5));
            
            //add new order which will set account to stop out
            _matchingEngine.SetOrders(MarketMaker1Id,
                new []{new LimitOrder { CreateDate = DateTime.UtcNow, Id = "7", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 790.286M, Volume = 15000 }
            }, new[] { "6" });

            Assert.AreEqual(4, account.GetOpenPositionsCount());
            Assert.AreEqual(376.81128742m, account.GetUsedMargin());

            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<MarginTradingAccount>(o => o.GetUsedMargin() == 376.81128742m && o.Balance == account.Balance)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountStopout(
                It.Is<string>(clientId => account.ClientId == clientId), 
                It.Is<string>(accountId => account.Id == accountId), It.Is<int>(count => count == 2), It.IsAny<decimal>()), Times.Once());
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.MarginCall,
                    It.Is<string>(message => message.Contains("Stop out")), null), Times.Once());
            _emailServiceMock.Verify(
                x => x.SendStopOutEmailAsync(It.IsAny<string>(), account.BaseAssetId, account.Id), Times.Once);
        }

        [Test]
        public void Is_MarginCall_Reached()
        {
            var ordersSet = new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.370M, Volume = -15000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 834.286M, Volume = 10000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet);

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCCHF", Bid = 905.57M, Ask = 905.67M }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Bid = 1.0092M, Ask = 1.0095M }));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "BTCCHF",
                Volume = 11.14644406903176M,  //10000 USD (with leverage)
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(_client1Id, _acount1Id);

            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.62683m, Math.Round(account.GetMarginUsageLevel(), 5));
            Assert.AreEqual(AccountLevel.None, account.GetAccountLevel()); //no margin call yet
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            //add new order which will set account to stop out
            _matchingEngine.SetOrders(MarketMaker1Id,
                new []{new LimitOrder { CreateDate = DateTime.UtcNow, Id = "7", Instrument = "BTCCHF", MarketMakerId = MarketMaker1Id, Price = 808.286M, Volume = 15000 }
            }, new[] { "6" });

            account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(AccountLevel.MarginCall, account.GetAccountLevel());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(
                x => x.SendNotification(It.IsAny<string>(), NotificationType.MarginCall, It.IsAny<string>(),
                    null), Times.Once());
            _emailServiceMock.Verify(
                x => x.SendMarginCallEmailAsync(It.IsAny<string>(), account.BaseAssetId, account.Id), Times.Once);
        }

        [Test]
        public void Check_No_Quote()
        {
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCJPY", Bid = 109.857M, Ask = 130.957M }));

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[1].Id,
                ClientId = _client1Id,
                Instrument = "BTCJPY",
                Volume = 1,
                FillType = OrderFillType.FillOrKill
            };

            Assert.ThrowsAsync<QuoteNotFoundException>(async () =>
            {
                order = await _tradingEngine.PlaceOrderAsync(order);
            });
        }

        [Test]
        public void Is_Balance_LessThanZero_On_StopOut_Thru_Big_Spread()
        {
            //set account balance to 50000 eur
            _accountManager.UpdateBalanceAsync(Accounts[1], 49000, AccountHistoryType.Deposit, "").Wait();

            var ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1097.315M, Volume = 100000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1125.945M, Volume = -100000 },
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[1].Id,
                ClientId = _client1Id,
                Instrument = "BTCEUR",
                Volume = 1000,
                FillType = OrderFillType.PartialFill
            };

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1125.945, order.OpenPrice);
            Assert.AreEqual(1097.315, order.ClosePrice);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(-28630, Math.Round(order.GetTotalFpl()));
            Assert.AreEqual(-28630, Math.Round(account.GetPnl()));

            ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1125.039M, Volume = 100000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1126.039M, Volume = -100000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);

            order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[1].Id,
                ClientId = _client1Id,
                Instrument = "BTCEUR",
                Volume = 1000,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(1126.039, order.OpenPrice);
            Assert.AreEqual(1125.039, order.ClosePrice);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(-1000, Math.Round(order.GetTotalFpl()));
            Assert.AreEqual(-1906, Math.Round(account.GetPnl()));


            //add orders to create big spread
            ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1097.315M, Volume = 100000 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "BTCEUR", MarketMakerId = MarketMaker1Id, Price = 1126.039M, Volume = -100000 }
            };

            _matchingEngine.SetOrders(MarketMaker1Id, ordersSet, deleteAll: true);

            Assert.IsTrue(account.Balance < 0);
        }

        #endregion

        #region Pending orders

        [Test]
        public void Is_PendingOrder_Rejected_On_Incorrect_ExpectedPrice()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                ExpectedOpenPrice = 0,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.InvalidExpectedOpenPrice, order.RejectReason);
        }

        [Test]
        public void Is_Buy_Partial_PendingOrder_Opened()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                ExpectedOpenPrice = 1.1M,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status);
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 6 }
            });

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.1125, order.OpenPrice);
            Assert.AreEqual(1, account.GetOpenPositionsCount());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Buy_Partial_PendingOrder_Opened_When_Available()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                ExpectedOpenPrice = 1.055M,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            Assert.IsTrue(account.GetUsedMargin() == 0); //no used margin
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = -6 }
            });

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.055M, Volume = -6 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //now its active
            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(1.056, Math.Round(order.OpenPrice, 3));
            Assert.AreEqual(-0.06, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(1, account.GetOpenPositionsCount()); //position is opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));

            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(message => message.Contains("Pending order triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

        }

        [Test]
        public void Is_Sell_Partial_PendingOrder_Opened()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -5,
                ExpectedOpenPrice = 1.05M,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
            
            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.06, order.OpenPrice);
            Assert.AreEqual(1, account.GetOpenPositionsCount());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Sell_Partial_PendingOrder_Opened_When_Available()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -1,
                ExpectedOpenPrice = 1.07M,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //now its active
            Assert.AreEqual(-0.02, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(1, account.GetOpenPositionsCount()); //position is opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Buy_FillOrKill_PendingOrder_Opened()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 1,
                ExpectedOpenPrice = 1.1M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status);
            
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.2M, Volume = 6 }
            });

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.1, order.OpenPrice);
            Assert.AreEqual(1.2, order.ClosePrice);
            Assert.AreEqual(1, account.GetOpenPositionsCount());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Buy_FillOrKill_PendingOrder_Opened_When_Available()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 1,
                ExpectedOpenPrice = 1.055M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = -6 }
            });

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.055M, Volume = -4 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //now its active
            Assert.AreEqual(-0.005, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(1, account.GetOpenPositionsCount()); //position is opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Sell_FillOrKill_PendingOrder_Opened()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -1,
                ExpectedOpenPrice = 1.05M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.06, order.OpenPrice);
            Assert.AreEqual(1, account.GetOpenPositionsCount());
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Sell_FillOrKill_PendingOrder_Opened_When_Available()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -1,
                ExpectedOpenPrice = 1.07M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new []
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //now its active
            Assert.AreEqual(-0.02, Math.Round(order.GetFpl(), 3));
            Assert.AreEqual(1, account.GetOpenPositionsCount()); //position is opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_Buy_PendingOrder_Rejected_On_Expected_Price()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = 8,
                ExpectedOpenPrice = 1.12M,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            //current ask price for EURUSD = 1.1 and ExpectedOpenPrice = 1.12, so order should be rejected
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.InvalidExpectedOpenPrice, order.RejectReason);
        }

        [Test]
        public void Is_Sell_PendingOrder_Rejected_On_Expected_Price()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -8,
                ExpectedOpenPrice = 1.04M,
                FillType = OrderFillType.PartialFill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;

            //current bid price for EURUSD = 1.05 and ExpectedOpenPrice = 1.04, so order should be rejected
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.InvalidExpectedOpenPrice, order.RejectReason);
        }

        [Test]
        public void Is_PendingOrder_Canceled()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -1,
                ExpectedOpenPrice = 1.07M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _tradingEngine.CancelPendingOrder(order.Id, OrderCloseReason.Canceled);

            Assert.AreEqual(OrderStatus.Closed, order.Status);
            Assert.AreEqual(OrderCloseReason.Canceled, order.CloseReason);
            Assert.IsNotNull(order.CloseDate);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        [Test]
        public void Is_PendingOrder_Rejected_On_Trigger()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -10000,
                ExpectedOpenPrice = 1.07M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.IsAny<string>(), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            Assert.AreEqual(OrderStatus.Rejected, order.Status); //should be rejected 
            Assert.AreEqual(OrderRejectReason.NoLiquidity, order.RejectReason);
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Rejected)));
        }

        [Test]
        public void Is_PendingOrder_Closed_After_Trigger()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = _acount1Id,
                ClientId = _client1Id,
                Instrument = "EURUSD",
                Volume = -5,
                ExpectedOpenPrice = 1.07M,
                FillType = OrderFillType.FillOrKill
            };

            order = _tradingEngine.PlaceOrderAsync(order).Result;
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //is not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.WaitingForExecution)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("placed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.06M, Volume = 6 }
            });

            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status); //still not active
            Assert.AreEqual(0, account.GetOpenPositionsCount()); //position is not opened

            _matchingEngine.SetOrders(MarketMaker1Id, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "6", Instrument = "EURUSD", MarketMakerId = MarketMaker1Id, Price = 1.08M, Volume = 10 }
            });

            Assert.AreEqual(OrderStatus.Active, order.Status); //should be active 
            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Active)));
            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionOpened, It.Is<string>(m => m.Contains("triggered")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());

            var closedOrder = _tradingEngine.CloseActiveOrderAsync(order.Id, OrderCloseReason.Close).Result;

            Assert.AreEqual(OrderStatus.Closed, closedOrder.Status); //should be closed 
            Assert.AreEqual(-0.1, order.GetTotalFpl());
            Assert.AreEqual(999.9, account.Balance);

            _clientNotifyServiceMock.Verify(x => x.NotifyOrderChanged(It.Is<Order>(o => o.Status == OrderStatus.Closed)));
            _clientNotifyServiceMock.Verify(x => x.NotifyAccountUpdated(It.Is<IMarginTradingAccount>(a => a.Balance == account.Balance)));

            _appNotificationsMock.Verify(x => x.SendNotification(It.IsAny<string>(), NotificationType.PositionClosed, It.Is<string>(message => message.Contains("closed")), It.Is<OrderHistoryBackendContract>(o => o.Id == order.Id)), Times.Once());
        }

        #endregion
    }

    public static class TestsExtension
    {
        public static void SetOrders(this IMarketMakerMatchingEngine matchingEngine, string marketMakerId, LimitOrder[] ordersToAdd = null, string[] orderIdsToDelete = null, bool deleteAll = false)
        {
            var model = new SetOrderModel
            {
                MarketMakerId = marketMakerId,
                OrdersToAdd = ordersToAdd,
                OrderIdsToDelete = orderIdsToDelete
            };

            if (deleteAll)
            {
                model.DeleteAllBuy = true;
                model.DeleteAllSell = true;
            }
            matchingEngine.SetOrders(model);
        }
    }
}
