using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
	public class OvernightSwapService : IOvernightSwapService
	{
		private readonly IOvernightSwapCache _overnightSwapCache;
		private readonly IAccountAssetsCacheService _accountAssetsCacheService;
		private readonly IAccountsCacheService _accountsCacheService;
		private readonly ICommissionService _commissionService;
		private readonly IOrderReader _orderReader;

		public OvernightSwapService(
			IOvernightSwapCache overnightSwapCache,
			IAccountAssetsCacheService accountAssetsCacheService,
			IAccountsCacheService accountsCacheService,
			ICommissionService commissionService,
			IOrderReader orderReader)
		{
			_overnightSwapCache = overnightSwapCache;
			_accountAssetsCacheService = accountAssetsCacheService;
			_accountsCacheService = accountsCacheService;
			_commissionService = commissionService;
			_orderReader = orderReader;
		}

		public void CalculateSwaps()
		{
			var failedOrders = new List<IOrder>();
			var openOrders = _orderReader.GetActive();

			foreach (var accountOrders in openOrders.GroupBy(x => x.AccountId))
			{
				var account = _accountsCacheService.Get(accountOrders.FirstOrDefault()?.ClientId, accountOrders.Key);
				if(account == null)
				{
					failedOrders.AddRange(accountOrders);
					continue;
				}
				
				foreach (var ordersByInstrument in accountOrders.GroupBy(x => x.Instrument))
				{
					var total = 0.0M;
					foreach (var order in ordersByInstrument)
					{
						var accountAssetPair =
							_accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);
						if (accountAssetPair == null)
						{
							failedOrders.Add(order);
							continue;	
						}

						total += _commissionService.GetOvernightSwap(order,
							order.GetOrderType() == OrderDirection.Buy 
								? accountAssetPair.CommissionLong 
								: accountAssetPair.CommissionShort);
					}
					
					var calculation = new OvernightSwapCalculation
					{
						AccountId = accountOrders.Key,
						Instrument = ordersByInstrument.Key,
						Timestamp = DateTime.UtcNow,
						Value = total,
						OpenOrderIds = ordersByInstrument.Select(x => x.Id).ToList()
					};
					
					//create transaction
					_overnightSwapCache.TryAdd(calculation);
				}
			}
			
		}
	}
}