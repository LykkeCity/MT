using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingStDevVectorRepository
    {
		Task Save(Dictionary<string, double> stDevVector);
	}
}
