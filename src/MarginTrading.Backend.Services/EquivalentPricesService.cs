using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
	public class EquivalentPricesService : IEquivalentPricesService
	{
		private readonly ICfdCalculatorService _cfdCalculatorService;
		private readonly MarginSettings _marginSettings;
		private readonly ILog _log;

		public EquivalentPricesService(
			ICfdCalculatorService cfdCalculatorService,
			MarginSettings marginSettings,
			ILog log)
		{
			_cfdCalculatorService = cfdCalculatorService;
			_marginSettings = marginSettings;
			_log = log;
		}

		private string GetEquivalentAsset()
		{
			return _marginSettings.ReportingEquivalentPricesAsset;
		}

		public void EnrichOpeningOrder(Order order)
		{
			order.EquivalentAsset = GetEquivalentAsset();
			
			try
			{
				order.OpenPriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
					order.Instrument);
			}
			catch (Exception e)
			{
				_log.WriteError("EnrichOpeningOrder", order.ToJson(), e);
			}
		}

		public void EnrichClosingOrder(Order order)
		{
			if (string.IsNullOrEmpty(order.EquivalentAsset))
			{
				order.EquivalentAsset = GetEquivalentAsset();
			}

			try
			{
				order.ClosePriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
					order.Instrument);
			}
			catch (Exception e)
			{
				_log.WriteError("EnrichClosingOrder", order.ToJson(), e);
			}
			
			
		}
	}
}