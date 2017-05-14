using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IPositionCacheService
	{
		Task Initialize(Func<Task<IEnumerable<IPosition>>> source);

		Task InitializeFromTransactions(Func<Task<IEnumerable<IElementaryTransaction>>> source);

		void UpdatePosition(IPosition position);

		IEnumerable<IPosition> GetPositions();

		IEnumerable<IPosition> GetPositions(string party);

		IPosition GetPosition(string party, string asset);

		bool IsInitialized { get; }

		IEnumerable<string> ClientIDs { get; }

		IEnumerable<string> Currencies { get; }
	}
}