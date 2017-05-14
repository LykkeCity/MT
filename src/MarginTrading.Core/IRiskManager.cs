using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IRiskManager
	{
		Task CheckLimits(IDictionary<string, double> pVaR);

		Task CheckLimit(string counterPartyId, double pVaREstimate);
	}
}
