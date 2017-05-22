using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Services.Events;
using System.Threading.Tasks;
using Lykke.Common;

namespace MarginTrading.Services
{
	public class RiskCalculationEngine : TimerPeriod,  IRiskCalculationEngine, IEventConsumer<ElementaryTransactionEventArgs>
	{
		private readonly IThreadSwitcher _threadSwitcher;
		private readonly ISampleQuoteCacheService _sampleQuoteCacheService;
		private readonly IPositionService _positionService;
		private readonly IRiskCalculator _riskCalculator;
		private readonly IRiskManager _riskManager;
		private readonly IMarginTradingMeanVectorRepository _meanVectorRepository;
		private readonly IMarginTradingPearsonCorrMatrixRepository _corrMatrixRepository;
		private readonly IMarginTradingStDevVectorRepository _stDevVectorRepository;
		private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
		private readonly IMarginTradingAggregateValuesAtRiskRepository _pVaRRepository;
		private readonly IMarginTradingIndividualValuesAtRiskRepository _iVaRRepository;
		private bool _initialized;

		public int ConsumerRank => 1;

		public RiskCalculationEngine(
				ISampleQuoteCacheService sampleQuoteCacheService,
				IPositionService positionService,
				IRiskCalculator riskCalculator,
				IRiskManager riskManager,
				IMarginTradingMeanVectorRepository meanVectorRepository,
				IMarginTradingPearsonCorrMatrixRepository corrMatrixRepository,
				IMarginTradingStDevVectorRepository stDevVectorRepository,
				IRabbitMqNotifyService rabbitMqNotifyService,
				IThreadSwitcher threadSwitcher,
				IMarginTradingAggregateValuesAtRiskRepository pVaRRepository,
				IMarginTradingIndividualValuesAtRiskRepository iVaRRepository,
				int samplingInterval,
				ILog log
			) : base(nameof(RiskCalculationEngine), samplingInterval, log)
		{
			_sampleQuoteCacheService = sampleQuoteCacheService;
			_positionService = positionService;
			_riskCalculator = riskCalculator;
			_riskManager = riskManager;
			_meanVectorRepository = meanVectorRepository;
			_corrMatrixRepository = corrMatrixRepository;
			_stDevVectorRepository = stDevVectorRepository;
			_threadSwitcher = threadSwitcher;
			_initialized = false;
			_rabbitMqNotifyService = rabbitMqNotifyService;
			_pVaRRepository = pVaRRepository;
			_iVaRRepository = iVaRRepository;
		}

		public async Task InitializeAsync()
		{
			await _sampleQuoteCacheService.InitializeAsync();

			await _positionService.InitializeAsync();

			_riskCalculator.Initialize(
				_positionService.ClientIDs,
				_positionService.Currencies,
				_sampleQuoteCacheService.GetMeanUsdQuoteVector,
				_sampleQuoteCacheService.GetLatestUsdQuote,
				_positionService.GetEquivalentUsdPosition);

			await _riskManager.CheckLimits(_riskCalculator.PVaR);

			await _stDevVectorRepository.Save(_riskCalculator.StDevLogReturns);

			await _meanVectorRepository.Save(_riskCalculator.MeanLogReturns);

			await _corrMatrixRepository.Save(_riskCalculator.PearsonCorrMatrix);

			foreach (var kvp1 in _riskCalculator.IVaR)
			{
				foreach (var kvp2 in kvp1.Value)
				{
					await _iVaRRepository.InsertOrUpdateAsync(kvp1.Key, kvp2.Key, kvp2.Value);
				}
			}

			foreach (var kvp in _riskCalculator.PVaR)
			{
				await _pVaRRepository.InsertOrUpdateAsync(kvp.Key, kvp.Value);
			}

			_initialized = true;
		}

		public async Task UpdateInternalStateAsync()
		{
			await _sampleQuoteCacheService.RunCacheUpdateAsync();

			_riskCalculator.UpdateInternalState(
				_sampleQuoteCacheService.GetMeanUsdQuoteVector,
				_sampleQuoteCacheService.GetLatestUsdQuote,
				_positionService.GetEquivalentUsdPosition);

			foreach (var kvp1 in _riskCalculator.IVaR)
			{
				foreach (var kvp2 in kvp1.Value)
				{
					await _iVaRRepository.InsertOrUpdateAsync(kvp1.Key, kvp2.Key, kvp2.Value);
				}
			}

			foreach (var kvp in _riskCalculator.PVaR)
			{
				await _pVaRRepository.InsertOrUpdateAsync(kvp.Key, kvp.Value);
			}

			await _riskManager.CheckLimits(_riskCalculator.PVaR);

			await _stDevVectorRepository.Save(_riskCalculator.StDevLogReturns);

			await _meanVectorRepository.Save(_riskCalculator.MeanLogReturns);

			await _corrMatrixRepository.Save(_riskCalculator.PearsonCorrMatrix);
		}

		public async Task ProcessTransactionAsync(IElementaryTransaction transaction)
		{
			var position = _positionService.ProcessTransaction(transaction);

			await _riskCalculator.RecalculatePVaRAsync(transaction.CounterPartyId, transaction.Asset,
				_positionService.GetEquivalentUsdPosition(transaction.CounterPartyId, transaction.Asset, _sampleQuoteCacheService.GetLatestUsdQuote));

			await _riskManager.CheckLimit(transaction.CounterPartyId, _riskCalculator.PVaR[transaction.CounterPartyId]);

			if (position != null)
				await _rabbitMqNotifyService.PositionUpdated(position);
		}

		public async override Task Execute()
		{
			if (_initialized)
			{
				await UpdateInternalStateAsync();
			}
		}

		public async override void Start()
		{
			base.Start();
			await InitializeAsync();
		}

		public void ConsumeEvent(object sender, ElementaryTransactionEventArgs ea)
		{
			var elementaryTransaction = ea.ElementaryTransaction;
			_threadSwitcher.SwitchThread(async () =>
			{
				await ProcessTransactionAsync(elementaryTransaction);
			});
		}
	}
}