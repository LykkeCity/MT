using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountNewHistoryBackendResponse
    {
        public AccountHistoryItemBackend[] HistoryItems { get; set; }
    }
}
