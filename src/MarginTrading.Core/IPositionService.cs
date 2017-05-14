using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IPositionService
	{
		IEnumerable<string> ClientIDs { get; }

		IEnumerable<string> Currencies { get; }

		Task InitializeAsync();

		double? GetEquivalentUsdPosition(string clientId, string currency, Func<string, OrderDirection, double?> getCurrentUsdQuoteForAsset);

		IPosition ProcessTransaction(IElementaryTransaction transaction);

		Task SavePositions();
	}
}