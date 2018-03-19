using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Backend.Contracts.Account
{
    public class AccountNewHistoryBackendResponse
    {
        public AccountHistoryItemBackend[] HistoryItems { get; set; }
    }
}
