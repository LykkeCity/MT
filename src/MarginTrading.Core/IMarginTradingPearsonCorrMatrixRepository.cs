using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingPearsonCorrMatrixRepository
    {
		Task Save(Dictionary<string, Dictionary<string, double>> corrMatrix);
	}
}