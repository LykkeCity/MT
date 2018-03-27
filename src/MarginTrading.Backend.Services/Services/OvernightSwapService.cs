using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Services;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Services
{
	/// <summary>
	/// Take care of overnight swap calculation and charging.
	/// </summary>
	public class OvernightSwapService : IOvernightSwapService
	{
		private readonly IOvernightSwapCache _overnightSwapCache;
		private readonly IAssetPairsCache _assetPairsCache;
		private readonly IAccountAssetsCacheService _accountAssetsCacheService;
		private readonly IAccountsCacheService _accountsCacheService;
		private readonly ICommissionService _commissionService;
		private readonly IOvernightSwapStateRepository _overnightSwapStateRepository;
		private readonly IOvernightSwapHistoryRepository _overnightSwapHistoryRepository;
		private readonly IOrderReader _orderReader;
		private readonly IThreadSwitcher _threadSwitcher;
		private readonly IDateService _dateService;
		private readonly AccountManager _accountManager;
		private readonly MarginSettings _marginSettings;
		private readonly ILog _log;

		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		private DateTime _currentStartTimestamp;

		public OvernightSwapService(
			IOvernightSwapCache overnightSwapCache,
			IAssetPairsCache assetPairsCache,
			IAccountAssetsCacheService accountAssetsCacheService,
			IAccountsCacheService accountsCacheService,
			ICommissionService commissionService,
			IOvernightSwapStateRepository overnightSwapStateRepository,
			IOvernightSwapHistoryRepository overnightSwapHistoryRepository,
			IOrderReader orderReader,
			IThreadSwitcher threadSwitcher,
			IDateService dateService,
			AccountManager accountManager,
			MarginSettings marginSettings,
			ILog log)
		{
			_overnightSwapCache = overnightSwapCache;
			_assetPairsCache = assetPairsCache;
			_accountAssetsCacheService = accountAssetsCacheService;
			_accountsCacheService = accountsCacheService;
			_commissionService = commissionService;
			_overnightSwapStateRepository = overnightSwapStateRepository;
			_overnightSwapHistoryRepository = overnightSwapHistoryRepository;
			_orderReader = orderReader;
			_threadSwitcher = threadSwitcher;
			_dateService = dateService;
			_accountManager = accountManager;
			_marginSettings = marginSettings;
			_log = log;
		}

		public void Start()
		{
			//initialize cache from storage
			var savedState = _overnightSwapStateRepository.GetAsync().GetAwaiter().GetResult().ToList();
			_overnightSwapCache.Initialize(savedState.Select(OvernightSwapCalculation.Create));
			
			//start calculation
			CalculateAndChargeSwaps();
			
			//TODO if server was down more that a day.. calc N days
		}

		/// <summary>
		/// Filter orders that are already calculated
		/// </summary>
		/// <returns></returns>
		private IEnumerable<Order> GetOrdersForCalculation()
		{
			//read orders syncronously
			var openOrders = _orderReader.GetActive();
			
			//prepare the list of orders
			var lastInvocationTime = CalcLastInvocationTime();
			var lastCalculation = _overnightSwapCache.GetAll().Where(x => x.Time > lastInvocationTime).ToList();
			var calculatedIds = lastCalculation.Where(x => x.IsSuccess).SelectMany(x => x.OpenOrderIds).ToHashSet();
			//select only non-calculated orders, changed before "official invocation time"
			var filteredOrders = openOrders.Where(x => (x.OpenDate ?? DateTime.MaxValue) < lastInvocationTime
			                                           && !calculatedIds.Contains(x.Id));

			//detect orders for which last calculation failed and it was closed
			var failedClosedOrders = lastCalculation.Where(x => !x.IsSuccess).SelectMany(x => x.OpenOrderIds)
				.Except(openOrders.Select(y => y.Id)).ToList();
			if (failedClosedOrders.Any())
			{
				_log.WriteErrorAsync(nameof(OvernightSwapService), nameof(GetOrdersForCalculation), new Exception(
						$"Overnight swap calculation failed for some orders and they were closed before recalculation: {string.Join(", ", failedClosedOrders)}."),
					DateTime.UtcNow).GetAwaiter().GetResult();
			}
			
			return filteredOrders;
		}

		public void CalculateAndChargeSwaps()
		{
			_currentStartTimestamp = _dateService.Now();

			var filteredOrders = GetOrdersForCalculation();
			
			//start calculation in a separate thread
			_threadSwitcher.SwitchThread(async () =>
			{
				await _semaphore.WaitAsync();

				try
				{
					foreach (var accountOrders in filteredOrders.GroupBy(x => x.AccountId))
					{
						var clientId = accountOrders.First().ClientId;
						MarginTradingAccount account;
						try
						{
							account = _accountsCacheService.Get(clientId, accountOrders.Key);
						}
						catch (Exception ex)
						{
							await ProcessFailedOrders(accountOrders, accountOrders.Key, null, ex);
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
								if (orders.Count == 0)
									continue;

								try
								{
									await ProcessOrders(orders, ordersByInstrument.Key, account, accountAssetPair, direction);
								}
								catch (Exception ex)
								{
									await ProcessFailedOrders(orders, account.Id, ordersByInstrument.Key, ex);
								}
							}
						}
					}
				}
				finally
				{
					_semaphore.Release();
				}
			});
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
		private async Task ProcessOrders(IReadOnlyList<Order> orders, string instrument, IMarginTradingAccount account,
				IAccountAssetPair accountAssetPair, OrderDirection direction)
		{
			IReadOnlyList<Order> filteredOrders = orders.ToList();
			
			//check if swaps had already been taken
			var lastCalcExists = _overnightSwapCache.TryGet(OvernightSwapCalculation.GetKey(account.Id, instrument, direction),
				                     out var lastCalc)
			                     && lastCalc.Time > CalcLastInvocationTime();
			if (lastCalcExists)
			{
				await _log.WriteErrorAsync(nameof(OvernightSwapService), nameof(ProcessOrders), 
					new Exception($"Overnight swaps had already been taken, filtering: {JsonConvert.SerializeObject(lastCalc)}"), DateTime.UtcNow);
				
				filteredOrders = orders.Where(x => !lastCalc.OpenOrderIds.Contains(x.Id)).ToList();
			}

			//calc swaps
			var swapRate = direction == OrderDirection.Buy ? accountAssetPair.OvernightSwapLong : accountAssetPair.OvernightSwapShort;
			if (swapRate == 0)
				return;
			
			var total = filteredOrders.Sum(order => _commissionService.GetOvernightSwap(order, swapRate));
			if (total == 0)
				return;
			
			//create calculation obj
			var calculation = OvernightSwapCalculation.Create(account.Id, instrument,
				filteredOrders.Select(order => order.Id).ToList(), _currentStartTimestamp, true, null, total, swapRate, direction);
	
			//charge comission
			var instrumentName = _assetPairsCache.TryGetAssetPairById(accountAssetPair.Instrument)?.Name 
			                     ?? accountAssetPair.Instrument;
			await _accountManager.UpdateBalanceAsync(
				account: account, 
				amount: - total, 
				historyType: AccountHistoryType.Swap,
				comment : $"{instrumentName} {(direction == OrderDirection.Buy ? "long" : "short")} swaps. Volume: {filteredOrders.Select(x => Math.Abs(x.Volume)).Sum()}. Positions count: {filteredOrders.Count}. Rate: {swapRate}. Time: {_currentStartTimestamp:u}.",
				auditLog: calculation.ToJson());
			
			//update calculation state if previous existed
			var newCalcState = lastCalcExists
				? OvernightSwapCalculation.Update(calculation, lastCalc)
				: OvernightSwapCalculation.Create(calculation);

			//add to cache
			_overnightSwapCache.AddOrReplace(newCalcState);
			
			//write state and log
			await _overnightSwapStateRepository.AddOrReplaceAsync(newCalcState);
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
		/// Return last invocation time. Take into account, that scheduler might fire the job with delay of 100ms.
		/// </summary>
		private DateTime CalcLastInvocationTime()
		{
			var dt = _dateService.Now();
			return new DateTime(dt.Year, dt.Month, dt.Day, _marginSettings.OvernightSwapCalculationHour, 0, 0)
				.ToUniversalTime()
				.AddDays(dt.Hour >= _marginSettings.OvernightSwapCalculationHour ? 0 : -1)
				.AddMilliseconds(100);
		}
	}
}