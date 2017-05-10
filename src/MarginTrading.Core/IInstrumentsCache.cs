using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IInstrumentsCache
    {
        IMarginTradingAsset GetInstrumentById(string instrumentId);
        IEnumerable<IMarginTradingAsset> GetAll();
        IMarginTradingAsset FindInstrument(string asset1, string asset2);
    }
}