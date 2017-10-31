using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class AccountBalanceChangedEventArgs
    {
        public AccountBalanceChangedEventArgs([NotNull] MarginTradingAccount account)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
        }

        public MarginTradingAccount Account { get; }
    }
}
