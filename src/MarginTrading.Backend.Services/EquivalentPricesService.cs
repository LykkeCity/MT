using System;
using System.Linq;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
	public class EquivalentPricesService : IEquivalentPricesService
	{
		private readonly IAccountsCacheService _accountsCacheService;
		private readonly ICfdCalculatorService _cfdCalculatorService;
		private readonly MarginTradingSettings _marginSettings;
		private readonly ILog _log;

		public EquivalentPricesService(
			IAccountsCacheService accountsCacheService,
			ICfdCalculatorService cfdCalculatorService,
			MarginTradingSettings marginSettings,
			ILog log)
		{
			_accountsCacheService = accountsCacheService;
			_cfdCalculatorService = cfdCalculatorService;
			_marginSettings = marginSettings;
			_log = log;
		}

		public void EnrichOpeningOrder(Position order)
		{
			try
			{
				order.OpenPriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
					order.Instrument, order.LegalEntity);
			}
			catch (Exception e)
			{
				_log.WriteError("EnrichOpeningOrder", order.ToJson(), e);
			}
		}

		public void EnrichClosingOrder(Position order)
		{
			try
			{
				order.ClosePriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
					order.Instrument, order.LegalEntity);
			}
			catch (Exception e)
			{
				_log.WriteError("EnrichClosingOrder", order.ToJson(), e);
			}
			
			
		}
	}
}