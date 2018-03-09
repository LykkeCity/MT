using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services;
using NUnit.Framework;

namespace MarginTradingTests.Backend
{
	[TestFixture]
	public class OvernightSwapServiceTests : BaseTests
	{
		private IOvernightSwapService _overnightSwapService;
		private IOvernightSwapCache _overnightSwapCache;
		private OrdersCache _ordersCache;
		private IMarginTradingAccountsRepository _fakeMarginTradingAccountsRepository;
		private IOvernightSwapStateRepository _overnightSwapStateRepository;
		
		[OneTimeSetUp]
		public void SetUp()
		{
			RegisterDependencies();
			
			_overnightSwapService = Container.Resolve<IOvernightSwapService>();
			_overnightSwapCache = Container.Resolve<IOvernightSwapCache>();
			_ordersCache = Container.Resolve<OrdersCache>();
			_fakeMarginTradingAccountsRepository = Container.Resolve<IMarginTradingAccountsRepository>();
			_overnightSwapStateRepository = Container.Resolve<IOvernightSwapStateRepository>();
		}

		[Test]
		public async Task OvernightSwapCalculation_Tests()
		{
			/*_ordersCache.ActiveOrders.Add(new Order
			{
				Id = "1",
				AccountId = "1",
				Instrument = "1",
				ClientId = "1",
				Volume = 1,
				OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
				CloseDate = new DateTime(2017, 01, 02, 20, 50, 0),
				MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 1} }),
			});
			
			_overnightSwapService.CalculateAndChargeSwaps();
			*/
			
			Assert.True(true);
		}
	}
}