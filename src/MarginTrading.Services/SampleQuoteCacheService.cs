using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class SampleQuoteCacheService : ISampleQuoteCacheService
	{
		private readonly int _maxSampleCount;
		private readonly int _samplingInterval;
		private readonly ISampleQuoteCacheRepository _sampleQuoteCacheRepository;
		private readonly IMarginTradingAssetsRepository _assetsRepository;
		private readonly IQuoteCacheService _quoteCacheService;
		private readonly IQuoteHistoryRepository _quoteHistoryRepository;
		private ISampleQuoteCache _cache;

		public SampleQuoteCacheService(
				ISampleQuoteCacheRepository sampleQuoteCacheRepository,
				IMarginTradingAssetsRepository assetsRepository,
				IQuoteCacheService quoteCacheService,
				IQuoteHistoryRepository quoteHistoryRepository,
				ISampleQuoteCache sampleQuoteCache,
				int maxCount,
				int samplingInterval
			)
		{
			_sampleQuoteCacheRepository = sampleQuoteCacheRepository;
			_assetsRepository = assetsRepository;
			_quoteCacheService = quoteCacheService;
			_quoteHistoryRepository = quoteHistoryRepository;
			_cache = sampleQuoteCache;
			_maxSampleCount = maxCount;
			_samplingInterval = samplingInterval;
		}

		public async Task InitializeAsync()
		{
			if (_sampleQuoteCacheRepository.Any() && !(await SettingsChanged()))
			{
				await InitializeFromRepositoryAsync();
			}
			else
			{
				await InitializeFromHistoryAsync();
				await BackupToRepository();
			}
		}

		public async Task RunCacheUpdateAsync()
		{
			IEnumerable<IMarginTradingAsset> instruments = await _assetsRepository.GetAllAsync();

			foreach (IMarginTradingAsset instrument in instruments)
			{
				InstrumentBidAskPair quote;
				try
				{
					quote = _quoteCacheService.GetQuote(instrument.Id);
				}
				catch (QuoteNotFoundException)
				{
					quote = null;
				}

				if (quote != null)
				{
					_cache.Enqueue(quote.Instrument, quote.Ask, quote.Bid);
				}
				else
				{
					IQuote bidQuote = _cache.GetLatestQuote(instrument.Id, OrderDirection.Sell);
					_cache.Enqueue(bidQuote);

					IQuote askQuote = _cache.GetLatestQuote(instrument.Id, OrderDirection.Buy);
					_cache.Enqueue(askQuote);
				}
			}

			await BackupToRepository();
		}

		public double? GetLatestUsdQuote(string asset, OrderDirection side)
		{
			if (asset == "USD")
				return 1;

			string instrument = $"{asset}USD";

			InstrumentBidAskPair quote;

			try
			{
				quote = _quoteCacheService.GetQuote(instrument);
			}
			catch (QuoteNotFoundException)
			{
				quote = null;
			}

			if (quote != null)
			{
				if (side == OrderDirection.Buy)
					return quote.Ask;
				if (side == OrderDirection.Sell)
					return quote.Bid;
			}

			var usdQuote = _cache.GetLatestQuote(instrument, side);

			if (usdQuote != null)
				return usdQuote.Price;

			instrument = $"USD{asset}";

			try
			{
				quote = _quoteCacheService.GetQuote(instrument);
			}
			catch (QuoteNotFoundException)
			{
				quote = null;
			}

			if (quote != null)
			{
				if (side == OrderDirection.Buy)
					return 1 / quote.Bid;
				if (side == OrderDirection.Sell)
					return 1 / quote.Ask;
			}

			usdQuote = _cache.GetLatestQuote(instrument, side);

			return usdQuote != null ? usdQuote.Price : new double?();
		}

		public double[] GetMeanUsdQuoteVector(string asset)
		{
			if (asset == "USD")
			{
				double[] ones = new double[_maxSampleCount];

				for (int i = 0; i < _maxSampleCount; i++)
				{
					ones[i] = 1;
				}

				return ones;
			}

			string instrument = $"{asset}USD";

			double[] bidQuotes = _cache.GetQuotes(instrument, OrderDirection.Sell);

			double[] askQuotes = _cache.GetQuotes(instrument, OrderDirection.Buy);

			if ((bidQuotes == null || bidQuotes.Length == 0) && (askQuotes != null && askQuotes.Length > 0))
				return askQuotes;

			if ((askQuotes == null || askQuotes.Length == 0) && (bidQuotes != null && bidQuotes.Length > 0))
				return bidQuotes;

			if (bidQuotes != null && bidQuotes.Length > 0 && askQuotes != null && askQuotes.Length > 0)
			{
				int maxLength = bidQuotes.Length >= askQuotes.Length ? bidQuotes.Length : askQuotes.Length;

				double[] result = new double[maxLength];

				for (int i = 0; i < maxLength; i++)
				{
					if (i < bidQuotes.Length && i < askQuotes.Length)
					{
						result[i] = (bidQuotes[i] + askQuotes[i]) / 2;
					}
					else if (i < bidQuotes.Length)
					{
						result[i] = bidQuotes[i];
					}
					else
					{
						result[i] = askQuotes[i];
					}
				}

				return result;
			}
			else
			{
				instrument = $"USD{asset}";

				bidQuotes = _cache.GetQuotes(instrument, OrderDirection.Sell);

				askQuotes = _cache.GetQuotes(instrument, OrderDirection.Buy);

				if ((bidQuotes == null || bidQuotes.Length == 0) && (askQuotes != null && askQuotes.Length > 0))
					return askQuotes;

				if ((askQuotes == null || askQuotes.Length == 0) && (bidQuotes != null && bidQuotes.Length > 0))
					return bidQuotes;

				if (askQuotes == null && bidQuotes == null)
					return null;

				int maxLength = bidQuotes.Length >= askQuotes.Length ? bidQuotes.Length : askQuotes.Length;

				double[] result = new double[maxLength];

				for (int i = 0; i < maxLength; i++)
				{
					if (i < bidQuotes.Length && i < askQuotes.Length)
					{
						result[i] = (1 / bidQuotes[i] + 1 / askQuotes[i]) / 2;
					}
					else if (i < bidQuotes.Length)
					{
						result[i] = 1 / bidQuotes[i];
					}
					else
					{
						result[i] = 1 / askQuotes[i];
					}
				}

				return result;
			}
		}

		private async Task<bool> SettingsChanged()
		{
			int maxSampleCount = await _sampleQuoteCacheRepository.GetMaxCount();

			long samplingInterval = await _sampleQuoteCacheRepository.GetSampleInterval();

			return maxSampleCount != _maxSampleCount || samplingInterval != _samplingInterval;
		}

		private async Task InitializeFromRepositoryAsync()
		{
			_cache = await _sampleQuoteCacheRepository.Restore();
		}

		private async Task InitializeFromHistoryAsync()
		{
			IEnumerable<IMarginTradingAsset> instruments = await _assetsRepository.GetAllAsync();

			List<DateTime> samplingPoints = CalculateSamplingPoints();

			foreach (IMarginTradingAsset instrument in instruments)
			{
				foreach (DateTime samplingPoint in samplingPoints)
				{
					await GetAndEnqueueSamplePointPrice(samplingPoint, instrument.Id, OrderDirection.Sell);

					await GetAndEnqueueSamplePointPrice(samplingPoint, instrument.Id, OrderDirection.Buy);
				}
			}
		}

		private List<DateTime> CalculateSamplingPoints()
		{
			List<DateTime> samplingPoints = new List<DateTime>();

			int normalizedSamplingInterval = (_samplingInterval / 60000) * 60000; //Round down to the whole minute

			DateTime lastPoint = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0); //Round down to the latest hour

			for (int i = _maxSampleCount - 1; i >= 0; i--)
			{
				samplingPoints.Add(lastPoint.AddMilliseconds(-i * normalizedSamplingInterval));
			}

			return samplingPoints;
		}

		private async Task GetAndEnqueueSamplePointPrice(DateTime samplingPoint, string instrument, OrderDirection side)
		{
			double? sampledPrice = await GetSampledPriceAsync(samplingPoint, instrument, side);
			if (sampledPrice != null)
			{
				_cache.Enqueue(instrument, side, sampledPrice.Value);
			}
			else
			{
				var lastQuote = _cache.GetLatestQuote(instrument, side);
				if (lastQuote != null)
				{
					_cache.Enqueue(lastQuote);
				}
			}
		}

		private async Task<double?> GetSampledPriceAsync(DateTime samplingPoint, string instrument, OrderDirection direction)
		{
			double? price = null;

			for (int i = 0; i < 5; i++)
			{
				DateTime sampleTime = samplingPoint.AddMinutes(-i);
				price = await _quoteHistoryRepository.GetClosestQuoteAsync(instrument, direction, sampleTime.Ticks);

				if (price != null)
					break;
			}

			return price;
		}

		private async Task BackupToRepository()
		{
			await _sampleQuoteCacheRepository.Backup(_cache);
			await _sampleQuoteCacheRepository.SaveSettings(_maxSampleCount, _samplingInterval);
		}
	}
}