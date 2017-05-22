using System;
using System.Collections.Generic;

namespace MarginTrading.Core
{
	public interface IRiskCalculator
	{
		IDictionary<string, double> PVaR { get; }

		void InitializeAsync(
			IEnumerable<string> clientIDs,
			IEnumerable<string> currencies,
			Func<string, double[]> getMeanUsdQuoteVector,
			Func<string, OrderDirection, double?> getLatestUsdQuote,
			Func<string, string, Func<string, OrderDirection, double?>, double?> getEquivalentUsdPosition);

		void UpdateInternalStateAsync(Func<string, double[]> getMeanUsdQuoteVector,
			Func<string, OrderDirection, double?> getLatestUsdQuote,
			Func<string, string, Func<string, OrderDirection, double?>, double?> getEquivalentUsdPosition);

		void RecalculatePVaR(string counterPartyId, string asset, double? equivalentUsdPositionVolume);
	}
}
