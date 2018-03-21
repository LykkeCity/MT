using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services
{
	/// <summary>
	/// Take care of overnight swap calculation and charging.
	/// </summary>
	public class OvernightSwapService : IOvernightSwapService
	{
		private readonly IOvernightSwapCache _overnightSwapCache;
		private readonly IAccountAssetsCacheService _accountAssetsCacheService;
		private readonly IAccountsCacheService _accountsCacheService;
		private readonly ICommissionService _commissionService;
		private readonly IOvernightSwapStateRepository _overnightSwapStateRepository;
		private readonly IOvernightSwapHistoryRepository _overnightSwapHistoryRepository;
		private readonly IOrderReader _orderReader;
		private readonly AccountManager _accountManager;
		private readonly MarginSettings _marginSettings;
		private readonly ILog _log;

		private DateTime _currentStartTimestamp;

		public OvernightSwapService(
			IOvernightSwapCache overnightSwapCache,
			IAccountAssetsCacheService accountAssetsCacheService,
			IAccountsCacheService accountsCacheService,
			ICommissionService commissionService,
			IOvernightSwapStateRepository overnightSwapStateRepository,
			IOvernightSwapHistoryRepository overnightSwapHistoryRepository,
			IOrderReader orderReader,
			AccountManager accountManager,
			MarginSettings marginSettings,
			ILog log)
		{
			_overnightSwapCache = overnightSwapCache;
			_accountAssetsCacheService = accountAssetsCacheService;
			_accountsCacheService = accountsCacheService;
			_commissionService = commissionService;
			_overnightSwapStateRepository = overnightSwapStateRepository;
			_overnightSwapHistoryRepository = overnightSwapHistoryRepository;
			_orderReader = orderReader;
			_accountManager = accountManager;
			_marginSettings = marginSettings;
			_log = log;
		}

		public void Start()
		{
			var savedState = _overnightSwapStateRepository.GetAsync().GetAwaiter().GetResult();
			_overnightSwapCache.Initialize(savedState.Select(OvernightSwapCalculation.Create));
		}

		public async Task CalculateAndChargeSwaps()
		{
			_currentStartTimestamp = DateTime.UtcNow;
			//TODO save open order state on timer, recheck on app init if any invocation was missed, start it on app init if so.
			//TODO on calculation start use this cached orders state
			
			foreach (var accountOrders in _orderReader.GetActive().GroupBy(x => x.AccountId))
			{
				var clientId = accountOrders.First().ClientId;
				MarginTradingAccount account;
				try
				{
					account = _accountsCacheService.Get(clientId, accountOrders.Key);
				}
				catch (Exception ex)
				{
					await ProcessFailedOrders(accountOrders, accountOrders.FirstOrDefault()?.AccountId, null, ex);
					continue;
				}
				
				foreach (var ordersByInstrument in accountOrders.GroupBy(x => x.Instrument))
				{
					var firstOrder = ordersByInstrument.FirstOrDefault();
					IAccountAssetPair accountAssetPair;
					try
					{
						accountAssetPair = _accountAssetsCacheService.GetAccountAsset(
							firstOrder?.TradingConditionId, firstOrder?.AccountAssetId, firstOrder?.Instrument);
					}
					catch (Exception ex)
					{
						await ProcessFailedOrders(ordersByInstrument, account.Id, ordersByInstrument.Key, ex);
						continue;
					}
					
					foreach (OrderDirection direction in Enum.GetValues(typeof(OrderDirection)))
					{
						var orders = ordersByInstrument.Where(order => order.GetOrderType() == direction).ToList();
						try
						{
							await ProcessOrders(orders, ordersByInstrument.Key, account, accountAssetPair, direction);
						}
						catch (Exception ex)
						{
							await ProcessFailedOrders(orders, account.Id, ordersByInstrument.Key, ex);
							continue;
						}
					}
				}
			}
		}

		/// <summary>
		/// Calculate overnight swaps for account/instrument/direction order package.
		/// </summary>
		/// <param name="instrument"></param>
		/// <param name="account"></param>
		/// <param name="accountAssetPair"></param>
		/// <param name="direction"></param>
		/// <param name="orders"></param>
		/// <returns></returns>
		private async Task ProcessOrders(IReadOnlyList<Order> orders, string instrument, MarginTradingAccount account,
				IAccountAssetPair accountAssetPair, OrderDirection direction)
		{
			//check if swaps had already been taken
			if (_overnightSwapCache.TryGet(OvernightSwapCalculation.GetKey(account.Id, instrument, direction),
				    out var lastCalc)
			    && CheckIfCurrentStartInterval(lastCalc.Timestamp))
				throw new Exception($"Overnight swaps had already been taken: {JsonConvert.SerializeObject(lastCalc)}");

			//calc swaps
			var swapRate = direction == OrderDirection.Buy ? accountAssetPair.CommissionLong : accountAssetPair.CommissionShort;
			var total = orders.Sum(order => _commissionService.GetOvernightSwap(order, swapRate));
	
			//charge comission
			await _accountManager.UpdateBalanceAsync(account, total, AccountHistoryType.Swap,
				$"{accountAssetPair.Instrument} {(direction == OrderDirection.Buy ? "long" : "short")} swaps. Volume: {orders.Sum(o => o.Volume)}. Positions count: {orders.Count}. Rate: {swapRate}. Time: {_currentStartTimestamp:u}.");
					
			//create calculation obj & add to cache
			var calculation = OvernightSwapCalculation.Create(account.Id, instrument,
				orders.Select(order => order.Id).ToList(), _currentStartTimestamp, true, null, total, swapRate, direction);
			_overnightSwapCache.AddOrReplace(calculation);
			
			//write state and log
			await _overnightSwapStateRepository.AddOrReplaceAsync(calculation);
			await _overnightSwapHistoryRepository.AddAsync(calculation);
		}

		/// <summary>
		/// Log failed orders.
		/// </summary>
		/// <param name="orders"></param>
		/// <param name="accountId"></param>
		/// <param name="instrument"></param>
		/// <param name="exception"></param>
		/// <returns></returns>
		private async Task ProcessFailedOrders(IEnumerable<Order> orders, string accountId, string instrument,
			Exception exception)
		{
			var failedCalculation = OvernightSwapCalculation.Create(accountId, instrument, 
				orders.Select(o => o.Id).ToList(), _currentStartTimestamp, false, exception);
			
			await _log.WriteErrorAsync(nameof(OvernightSwapService), nameof(ProcessFailedOrders), 
				new Exception(failedCalculation.ToJson()), DateTime.UtcNow);

			await _overnightSwapHistoryRepository.AddAsync(failedCalculation);
		}

		/// <summary>
		/// Checks if last calculation time is in the current invocation time period.
		/// Takes into account, that scheduler might fire the job with delay of 100ms.
		/// https://github.com/fluentscheduler/FluentScheduler#milliseconds-accuracy
		/// </summary>
		/// <param name="lastTimestamp"></param>
		/// <returns></returns>
		private bool CheckIfCurrentStartInterval(DateTime lastTimestamp)
		{
			var lastCalcFixedTime = lastTimestamp.AddMilliseconds(-100);
			var lastInvocationTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
					_marginSettings.OvernightSwapCalculationHour, 0, 0)
				.AddDays(DateTime.UtcNow.Hour >= _marginSettings.OvernightSwapCalculationHour ? 0 : -1);
			return lastCalcFixedTime > lastInvocationTime;
		}
	}
}