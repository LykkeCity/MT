// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class StopOutEventArgs
    {
        public StopOutEventArgs(MarginTradingAccount account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            Account = account;
        }

        public MarginTradingAccount Account { get; }
    }
}