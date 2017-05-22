using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingMeanVectorRepository
    {
		Task Save(Dictionary<string, double> meansVector);
	}
}
