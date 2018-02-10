using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
	public class VolumeEquivalentService : IVolumeEquivalentService
	{
		private readonly ICfdCalculatorService _cfdCalculatorService;
		private readonly string _equivalentAssetSetting;

		public VolumeEquivalentService(
			ICfdCalculatorService cfdCalculatorService,
			string equivalentAssetSetting)
		{
			_cfdCalculatorService = cfdCalculatorService;
			_equivalentAssetSetting = equivalentAssetSetting;
		}

		private string GetEquivalentAsset()
		{
			if (string.IsNullOrEmpty(_equivalentAssetSetting))
				throw new Exception("Equivalent asset is not set in settings.");
			return _equivalentAssetSetting;
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
				order.EquivalentAsset = GetEquivalentAsset();
			order.ClosePriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				order.Instrument);
		}
	}
}