﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
	public class EquivalentPricesService : IEquivalentPricesService
	{
		private readonly ICfdCalculatorService _cfdCalculatorService;
		private readonly MarginSettings _marginSettings;

		public EquivalentPricesService(
			ICfdCalculatorService cfdCalculatorService,
			MarginSettings marginSettings)
		{
			_cfdCalculatorService = cfdCalculatorService;
			_marginSettings = marginSettings;
		}

		private string GetEquivalentAsset()
		{
			return _marginSettings.ReportingEquivalentPricesAsset;
		}

		public void EnrichOpeningOrder(Order order)
		{
			order.EquivalentAsset = GetEquivalentAsset();
			order.OpenPriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				order.Instrument);
		}

		public void EnrichClosingOrder(Order order)
		{
			if (string.IsNullOrEmpty(order.EquivalentAsset))
			{
				order.EquivalentAsset = GetEquivalentAsset();
			}

			order.ClosePriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				order.Instrument);
		}
	}
}