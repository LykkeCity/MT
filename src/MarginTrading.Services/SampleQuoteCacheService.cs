using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.Notifications;
using MarginTrading.Core.Settings;
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
		private readonly ISlackNotificationsProducer _slackNotificationProducer;
		private ISampleQuoteCache _cache;

		public SampleQuoteCacheService(
				ISampleQuoteCacheRepository sampleQuoteCacheRepository,
				IMarginTradingAssetsRepository assetsRepository,
				IQuoteCacheService quoteCacheService,
				IQuoteHistoryRepository quoteHistoryRepository,
				ISampleQuoteCache sampleQuoteCache,
				ISlackNotificationsProducer slackNotificationProducer,
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
			_slackNotificationProducer = slackNotificationProducer;
		}

		public async Task InitializeAsync()
		{
			if (_sampleQuoteCacheRepository.Any() && !(await SettingsChanged()))
			{
				await InitializeFromRepositoryAsync();
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
			CheckDataQuality(instruments);

			await BackupToRepository();
		}

		private void CheckDataQuality(IEnumerable<IMarginTradingAsset> instruments)
		{
			foreach (IMarginTradingAsset instrument in instruments)
			{
				if (_cache.GetQuotes(instrument.Id, OrderDirection.Buy) == null || _cache.GetQuotes(instrument.Id, OrderDirection.Buy).Length < _maxSampleCount)
				{
					_slackNotificationProducer.SendNotification(ChannelTypes.MarginTrading, $"Quote sample vector for the instrument {instrument.Id} in direction {OrderDirection.Buy} is not filled. iVaR calculation for this instrument may be inaccurate.", "Margin Trading Risk Management Module");
				}

				if (_cache.GetQuotes(instrument.Id, OrderDirection.Sell) == null || _cache.GetQuotes(instrument.Id, OrderDirection.Sell).Length < _maxSampleCount)
				{
					_slackNotificationProducer.SendNotification(ChannelTypes.MarginTrading, $"Quote sample vector for the instrument {instrument.Id} in direction {OrderDirection.Sell} is not filled. iVaR calculation for this instrument may be inaccurate.", "Margin Trading Risk Management Module");
				}
			}
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

			return usdQuote != null ? 1 / usdQuote.Price : new double?();
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

		private async Task BackupToRepository()
		{
			await _sampleQuoteCacheRepository.Backup(_cache);
			await _sampleQuoteCacheRepository.SaveSettings(_maxSampleCount, _samplingInterval);
		}
	}
}