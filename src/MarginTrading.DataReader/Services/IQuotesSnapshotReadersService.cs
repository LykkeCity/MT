using System.Collections.Generic;
using MarginTrading.Backend.Core;

namespace MarginTrading.DataReader.Services
{
    public interface IQuotesSnapshotReadersService
    {
        IReadOnlyDictionary<string, InstrumentBidAskPair> GetSnapshotAsync();
    }
}