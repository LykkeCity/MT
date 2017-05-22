using MarginTrading.Core;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class RiskCalculationEngine : IRiskCalculationEngine
	{
		private readonly ISampleQuoteCacheService _sampleQuoteCacheService;
		private readonly IPositionService _positionService;
		private readonly IRiskCalculator _riskCalculator;
		private readonly IRiskManager _riskManager;

		public RiskCalculationEngine(
				ISampleQuoteCacheService sampleQuoteCacheService,
				IPositionService positionService,
				IRiskCalculator riskCalculator,
				IRiskManager riskManager
			)
		{
			_sampleQuoteCacheService = sampleQuoteCacheService;
			_positionService = positionService;
			_riskCalculator = riskCalculator;
			_riskManager = riskManager;
		}

		public async Task InitializeAsync()
		{
			await _sampleQuoteCacheService.InitializeAsync();

			await _positionService.InitializeAsync();

			_riskCalculator.InitializeAsync(
				_positionService.ClientIDs,
				_positionService.Currencies,
				_sampleQuoteCacheService.GetMeanUsdQuoteVector,
				_sampleQuoteCacheService.GetLatestUsdQuote,
				_positionService.GetEquivalentUsdPosition);

			await _riskManager.CheckLimits(_riskCalculator.PVaR);
		}

		public async Task UpdateInternalStateAsync()
		{
			await _sampleQuoteCacheService.RunCacheUpdateAsync();

			_riskCalculator.UpdateInternalStateAsync(
				_sampleQuoteCacheService.GetMeanUsdQuoteVector,
				_sampleQuoteCacheService.GetLatestUsdQuote,
				_positionService.GetEquivalentUsdPosition);

			await _riskManager.CheckLimits(_riskCalculator.PVaR);
		}

		public async Task ProcessTransactionAsync(IElementaryTransaction transaction)
		{
			_positionService.ProcessTransaction(transaction);

			_riskCalculator.RecalculatePVaR(transaction.CounterPartyId, transaction.Asset,
				_positionService.GetEquivalentUsdPosition(transaction.CounterPartyId, transaction.Asset, _sampleQuoteCacheService.GetLatestUsdQuote));

			await _riskManager.CheckLimit(transaction.CounterPartyId, _riskCalculator.PVaR[transaction.CounterPartyId]);
		}
	}
}