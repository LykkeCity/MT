using System.Collections.Generic;

namespace MarginTrading.Core
{
	public interface ISampleQuoteCache
	{
		int MaxCount { get; }

		IDictionary<string, IQuote[]> Buy { get; }

		IDictionary<string, IQuote[]> Sell { get; }

		void Enqueue(IQuote quote);

		void Enqueue(string instrument, OrderDirection direction, double price);

		void Enqueue(string instrument, double buyQuote, double sellQuote);

		void Initialize(IEnumerable<IQuote> quotesCollection);

		void Initialize(IDictionary<string, IEnumerable<IQuote>> buyQuoteCache, IDictionary<string, IEnumerable<IQuote>> sellQuoteCache);

		double[] GetQuotes(string instrument, OrderDirection direction);

		IQuote GetLatestQuote(string instrument, OrderDirection direction);
	}
}