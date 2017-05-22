using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IRiskCalculator
	{
		IDictionary<string, double> PVaR { get; }
		IDictionary<string, Dictionary<string, double>> IVaR { get; }
		Dictionary<string, double> StDevLogReturns { get; }
		Dictionary<string, double> MeanLogReturns { get; }
		Dictionary<string, Dictionary<string, double>> PearsonCorrMatrix { get; }

		void Initialize(
			IEnumerable<string> clientIDs,
			IEnumerable<string> currencies,
			Func<string, double[]> getMeanUsdQuoteVector,
			Func<string, OrderDirection, double?> getLatestUsdQuote,
			Func<string, string, Func<string, OrderDirection, double?>, double?> getEquivalentUsdPosition);

		void UpdateInternalState(Func<string, double[]> getMeanUsdQuoteVector,
			Func<string, OrderDirection, double?> getLatestUsdQuote,
			Func<string, string, Func<string, OrderDirection, double?>, double?> getEquivalentUsdPosition);

		Task RecalculatePVaRAsync(string counterPartyId, string asset, double? equivalentUsdPositionVolume);
	}
}
