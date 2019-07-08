// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
