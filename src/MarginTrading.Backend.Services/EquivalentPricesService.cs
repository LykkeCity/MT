using System;
using System.Linq;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services
{
	public class EquivalentPricesService : IEquivalentPricesService
	{
		private readonly ICfdCalculatorService _cfdCalculatorService;
		private readonly ILog _log;

		public EquivalentPricesService(
			ICfdCalculatorService cfdCalculatorService,
			ILog log)
		{
			_cfdCalculatorService = cfdCalculatorService;
			_log = log;
		}

		public void EnrichOpeningOrder(Order order)
		{
			try
			{
				//order.OpenPriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				//	order.Instrument, order.LegalEntity);
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
				//order.ClosePriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				//	order.AssetPairId, order.LegalEntity);
			}
			catch (Exception e)
			{
				_log.WriteError("EnrichClosingOrder", order.ToJson(), e);
			}
			
			
		}
	}
}