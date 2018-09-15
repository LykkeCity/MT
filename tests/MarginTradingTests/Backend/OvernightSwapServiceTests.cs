using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Services;
using Moq;
using MoreLinq;
using NUnit.Framework;

namespace MarginTradingTests.Backend
{
	[TestFixture]
	public class OvernightSwapServiceTests : BaseTests
	{
		private IOvernightSwapService _overnightSwapService;
		private IOvernightSwapCache _overnightSwapCache;
		private IQuoteCacheService _quoteCacheService;
		private OrdersCache _ordersCache;
		private IAccountAssetPairsRepository _accountAssetsRepository;
		private IMarginTradingAccountsRepository _fakeMarginTradingAccountsRepository;
		private IOvernightSwapStateRepository _overnightSwapStateRepository;
		private IOvernightSwapHistoryRepository _overnightSwapHistoryRepository;
		private IRabbitMqNotifyService _rabbitMqNotifyService;
		private AccountAssetsManager _accountAssetsManager;
		private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			RegisterDependencies();
			
			_overnightSwapService = Container.Resolve<IOvernightSwapService>();
			_overnightSwapCache = Container.Resolve<IOvernightSwapCache>();
			_quoteCacheService = Container.Resolve<IQuoteCacheService>();
			_ordersCache = Container.Resolve<OrdersCache>();
			_accountAssetsRepository = Container.Resolve<IAccountAssetPairsRepository>();
			_fakeMarginTradingAccountsRepository = Container.Resolve<IMarginTradingAccountsRepository>();
			_overnightSwapStateRepository = Container.Resolve<IOvernightSwapStateRepository>();
			_overnightSwapHistoryRepository = Container.Resolve<IOvernightSwapHistoryRepository>();
			_rabbitMqNotifyService = Container.Resolve<IRabbitMqNotifyService>();
			_accountAssetsManager = Container.Resolve<AccountAssetsManager>();
			_bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
		}

		[SetUp]
		public void SetUp()
		{
			_accountAssetsRepository.GetAllAsync().Result.ForEach(aa => 
				_accountAssetsRepository.Remove(aa.TradingConditionId, aa.BaseAssetId, "BTCUSD"));
			_accountAssetsManager.UpdateAccountAssetsCache().GetAwaiter().GetResult();
			
			_ordersCache.InitOrders(new List<Order>());
			
			_overnightSwapCache.ClearAll();
			
			_quoteCacheService.GetAllQuotes().Values.ForEach(x => _quoteCacheService.RemoveQuote(x.Instrument));
			
			_overnightSwapHistoryRepository.GetAsync().Result.ForEach(x => 
				_overnightSwapHistoryRepository.DeleteAsync(x));
		}

		[TearDown]
		public void TearDown()
		{
			SetUp();
		}

		[Test]
		public async Task OvernightSwapCalculation_Success()
		{
			var dsMock = Mock.Get(Container.Resolve<IDateService>());
			dsMock.Setup(d => d.Now()).Returns(new DateTime(2017, 1, 2));
			
			await _accountAssetsRepository.AddOrReplaceAsync(new AccountAssetPair
			{
				TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
				BaseAssetId = "USD",
				Instrument = "BTCUSD",
				LeverageInit = 100,
				LeverageMaintenance = 150,
				SwapLong = 100,
				SwapShort = 100,
				OvernightSwapLong = 1,
				OvernightSwapShort = 1
			});

			await _accountAssetsManager.UpdateAccountAssetsCache();

			var accountId = (await _fakeMarginTradingAccountsRepository.GetAllAsync("1"))
				.First(x => x.ClientId == "1" && x.BaseAssetId == "USD").Id;
			
			_ordersCache.ActiveOrders.Add(new Order
			{
				Id = "1",
				AccountId = accountId,
				Instrument = "BTCUSD",
				ClientId = "1",
				TradingConditionId = "1",
				AccountAssetId = "USD",
				Volume = 1,
				OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
				MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 1} }),
				LegalEntity = "LYKKETEST",
			});
			
			_ordersCache.ActiveOrders.Add(new Order
			{
				Id = "2",
				AccountId = accountId,
				Instrument = "BTCUSD",
				ClientId = "1",
				TradingConditionId = "1",
				AccountAssetId = "USD",
				Volume = 1,
				OpenDate = new DateTime(2017, 01, 02, 20, 50, 0),
				MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 1} }),
				LegalEntity = "LYKKETEST",
			});

			const decimal swapValue = 0.00273973M;
			
			var initialAccountBalance = (await _fakeMarginTradingAccountsRepository.GetAsync(accountId)).Balance;
			
			_overnightSwapService.CalculateAndChargeSwaps();

			var calculations = _overnightSwapCache.GetAll();
			Assert.AreEqual(1, calculations.Count); //only order 1 should be calculated
			var singleCalc = calculations.First();
			Assert.AreEqual(swapValue, singleCalc.Value);
			Assert.True(singleCalc.IsSuccess);
			Assert.AreEqual(initialAccountBalance - swapValue, (await _fakeMarginTradingAccountsRepository.GetAsync(accountId)).Balance);
			
			//move few hours later and calculate again
			dsMock.Setup(d => d.Now()).Returns(new DateTime(2017, 1, 2, 2, 0, 0));
			
			//nothing should change
			calculations = _overnightSwapCache.GetAll();
			Assert.AreEqual(1, calculations.Count);
			Assert.AreEqual(initialAccountBalance - swapValue, (await _fakeMarginTradingAccountsRepository.GetAsync(accountId)).Balance);
			
			_overnightSwapService.CalculateAndChargeSwaps();
			
			//move to next day and calculate again
			dsMock.Setup(d => d.Now()).Returns(new DateTime(2017, 1, 3));
			
			_overnightSwapService.CalculateAndChargeSwaps();
			
			calculations = _overnightSwapCache.GetAll();
			Assert.AreEqual(2, calculations.Count); // both order 1 and order 2 should be calculated
			Assert.AreEqual(initialAccountBalance - swapValue * 3,
				(await _fakeMarginTradingAccountsRepository.GetAsync(accountId)).Balance);
		}

		[Test]
		public async Task OvernightSwapCalculation_Fail_NoQuotes()
		{ 
			await _accountAssetsRepository.AddOrReplaceAsync(new AccountAssetPair
			{
				TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
				BaseAssetId = "CHF",
				Instrument = "BTCUSD",
				LeverageInit = 100,
				LeverageMaintenance = 150,
				SwapLong = 100,
				SwapShort = 100,
				OvernightSwapLong = 1,
				OvernightSwapShort = 1
			});

			await _accountAssetsManager.UpdateAccountAssetsCache();
			
			var accountId = (await _fakeMarginTradingAccountsRepository.GetAllAsync("1"))
				.First(x => x.ClientId == "1" && x.BaseAssetId == "CHF").Id;
			_ordersCache.ActiveOrders.Add(new Order
			{
				Id = "1",
				AccountId = accountId,
				Instrument = "BTCUSD",
				ClientId = "1",
				TradingConditionId = "1",
				AccountAssetId = "CHF",
				Volume = 1,
				OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
				CloseDate = new DateTime(2017, 01, 02, 20, 50, 0),
				MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 1} }),
				LegalEntity = "LYKKETEST",
			});
			
			_overnightSwapService.CalculateAndChargeSwaps();

			var history = (await _overnightSwapHistoryRepository.GetAsync()).First(x => x.AccountId == accountId && x.Exception != null);
			
			Assert.False(history.IsSuccess);
			Assert.AreEqual("There is no quote for instrument USDCHF", ((IOvernightSwapHistory)history).Exception.Message);
		}
		
		[Test]
		public async Task OvernightSwapCalculation_Fail_NoAccountAsset()
		{
			_bestPriceConsumer.SendEvent(this,
				new BestPriceChangeEventArgs(new InstrumentBidAskPair {Instrument = "BTCUSD", Bid = 9000M, Ask = 9010M}));

			var accountId = (await _fakeMarginTradingAccountsRepository.GetAllAsync("1"))
				.First(x => x.ClientId == "1" && x.BaseAssetId == "EUR").Id;
			_ordersCache.ActiveOrders.Add(new Order
			{
				Id = "1",
				AccountId = accountId,
				Instrument = "BTCUSD",
				ClientId = "1",
				TradingConditionId = "1",
				AccountAssetId = "EUR",
				Volume = 1,
				OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
				CloseDate = new DateTime(2017, 01, 02, 20, 50, 0),
				MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 1} }),
			});
			
			_overnightSwapService.CalculateAndChargeSwaps();

			var history = (await _overnightSwapHistoryRepository.GetAsync()).First(x => x.AccountId == accountId);
			
			Assert.False(history.IsSuccess);
			Assert.AreEqual("Can't find AccountAsset for tradingConditionId: 1, baseAssetId: EUR, instrument: BTCUSD",
				((IOvernightSwapHistory)history).Exception.Message);
		}
	}
}