using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core
{
	public class SampleQuoteCache : ISampleQuoteCache
	{
		private Dictionary<string, Queue<IQuote>> _buy;

		private Dictionary<string, Queue<IQuote>> _sell;

		private int _maxCount;

		public int MaxCount
		{
			get
			{
				return _maxCount;
			}
		}

		public IDictionary<string, IQuote[]> Buy
		{
			get
			{
				Dictionary<string, IQuote[]> container = new Dictionary<string, IQuote[]>();

				foreach (KeyValuePair<string, Queue<IQuote>> kvp in _buy)
				{
					container.Add(kvp.Key, kvp.Value.ToArray());
				}

				return container;
			}
		}

		public IDictionary<string, IQuote[]> Sell
		{
			get
			{
				Dictionary<string, IQuote[]> container = new Dictionary<string, IQuote[]>();

				foreach (KeyValuePair<string, Queue<IQuote>> kvp in _sell)
				{
					container.Add(kvp.Key, kvp.Value.ToArray());
				}

				return container;
			}
		}

		public SampleQuoteCache(int maxCount)
		{
			_maxCount = maxCount;
		}

		public void Enqueue(IQuote quote)
		{
			if (quote.Direction == OrderDirection.Buy)
				EnqueueBuy(quote);
			else if (quote.Direction == OrderDirection.Sell)
				EnqueueSell(quote);
		}

		private void EnqueueBuy(IQuote quote)
		{
			if (_buy == null)
			{
				_buy = new Dictionary<string, Queue<IQuote>>();
			}

			if (!_buy.ContainsKey(quote.Instrument))
			{
				_buy[quote.Instrument] = new Queue<IQuote>();
			}

			Enqueue(quote, _buy[quote.Instrument]);
		}

		private void EnqueueSell(IQuote quote)
		{
			if (_sell == null)
			{
				_sell = new Dictionary<string, Queue<IQuote>>();
			}

			if (!_sell.ContainsKey(quote.Instrument))
			{
				_sell[quote.Instrument] = new Queue<IQuote>();
			}

			Enqueue(quote, _sell[quote.Instrument]);
		}

		private void Enqueue(IQuote quote, Queue<IQuote> queue)
		{
			if (queue.Count == _maxCount)
			{
				queue.Dequeue();
			}

			queue.Enqueue(quote);
		}

		public void Enqueue(string instrument, OrderDirection direction, double price)
		{
			Enqueue(new Quote
			{
				Instrument = instrument,
				Direction = direction,
				Price = price
			});
		}

		public void Enqueue(string instrument, double buyPrice, double sellPrice)
		{
			Enqueue(new Quote
			{
				Instrument = instrument,
				Direction = OrderDirection.Buy,
				Price = buyPrice
			});

			Enqueue(new Quote
			{
				Instrument = instrument,
				Direction = OrderDirection.Sell,
				Price = sellPrice
			});
		}

		public void Initialize(IEnumerable<IQuote> initializer)
		{
			foreach (IQuote quote in initializer)
			{
				Enqueue(quote);
			}
		}

		public void Initialize(IDictionary<string, IEnumerable<IQuote>> buyQuoteCache, IDictionary<string, IEnumerable<IQuote>> sellQuoteCache)
		{
			foreach (KeyValuePair<string, IEnumerable<IQuote>> buyQuoteCollectionItem in buyQuoteCache)
			{
				string instrument = buyQuoteCollectionItem.Key;

				foreach (IQuote quote in buyQuoteCollectionItem.Value)
				{
					Enqueue(quote);
				}
			}

			foreach (KeyValuePair<string, IEnumerable<IQuote>> sellQuoteCollectionItem in sellQuoteCache)
			{
				string instrument = sellQuoteCollectionItem.Key;

				foreach (IQuote quote in sellQuoteCollectionItem.Value)
				{
					Enqueue(quote);
				}
			}
		}

		public double[] GetQuotes(string instrument, OrderDirection direction)
		{
			if (direction == OrderDirection.Buy)
			{
				return GetQuotesBuy(instrument);
			}
			return GetQuotesSell(instrument);
		}

		private double[] GetQuotesBuy(string instrument)
		{
			if (_buy == null || _buy.ContainsKey(instrument))
			{
				return _buy[instrument].Select(x => x.Price).ToArray();
			}
			return null;
		}

		private double[] GetQuotesSell(string instrument)
		{
			if (_sell == null || _sell.ContainsKey(instrument))
			{
				return _sell[instrument].Select(x => x.Price).ToArray();
			}
			return null;
		}

		public IQuote GetLatestQuote(string instrument, OrderDirection direction)
		{
			if (direction == OrderDirection.Buy)
			{
				return GetLatestQuotesBuy(instrument);
			}
			return GetLatestQuotesSell(instrument);
		}

		private IQuote GetLatestQuotesBuy(string instrument)
		{
			if (_buy == null)
				return null;
			if (!_buy.ContainsKey(instrument))
				return null;
			return _buy[instrument].LastOrDefault();
		}

		private IQuote GetLatestQuotesSell(string instrument)
		{
			if (_sell == null)
				return null;
			if (!_sell.ContainsKey(instrument))
				return null;
			return _sell[instrument].LastOrDefault();
		}
	}
}