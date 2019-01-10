using System;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class MarginCallEventArgs
    {
        public MarginCallEventArgs(MarginTradingAccount account, AccountLevel level)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            MarginCallLevel = level;
        }

        public AccountLevel MarginCallLevel { get; }
        
        public MarginTradingAccount Account { get; }
    }
}