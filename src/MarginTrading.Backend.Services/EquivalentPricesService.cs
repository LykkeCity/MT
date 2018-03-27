using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services
{
	public class EquivalentPricesService : IEquivalentPricesService
	{
		private readonly IAccountsCacheService _accountsCacheService;
		private readonly ICfdCalculatorService _cfdCalculatorService;
		private readonly MarginSettings _marginSettings;

		public EquivalentPricesService(
			IAccountsCacheService accountsCacheService,
			ICfdCalculatorService cfdCalculatorService,
			MarginSettings marginSettings)
		{
			_accountsCacheService = accountsCacheService;
			_cfdCalculatorService = cfdCalculatorService;
			_marginSettings = marginSettings;
		}

		private string GetEquivalentAsset(string clientId, string accountId)
		{
			var account = _accountsCacheService.Get(clientId, accountId);
			var equivalentSettings =
				_marginSettings.ReportingEquivalentPricesSettings.FirstOrDefault(x => x.LegalEntity == account.LegalEntity);
			
			if(string.IsNullOrEmpty(equivalentSettings?.EquivalentAsset))
				throw new Exception($"No reporting equivalent prices asset found for legalEntity: {account.LegalEntity}");
			
			return equivalentSettings.EquivalentAsset;
		}

		public void EnrichOpeningOrder(Order order)
		{
			order.EquivalentAsset = GetEquivalentAsset(order.ClientId, order.AccountId);
			order.OpenPriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				order.Instrument);
		}

		public void EnrichClosingOrder(Order order)
		{
			if (string.IsNullOrEmpty(order.EquivalentAsset))
			{
				order.EquivalentAsset = GetEquivalentAsset(order.ClientId, order.AccountId);
			}

			order.ClosePriceEquivalent = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
				order.Instrument);
		}
	}
}