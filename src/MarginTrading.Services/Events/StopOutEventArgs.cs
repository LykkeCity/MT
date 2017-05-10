using System;
using MarginTrading.Core;

namespace MarginTrading.Services.Events
{
    public class StopOutEventArgs
    {
        public StopOutEventArgs(MarginTradingAccount account, Order[] orders)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (orders == null) throw new ArgumentNullException(nameof(orders));
            Account = account;
            Orders = orders;
        }

        public MarginTradingAccount Account { get; }
        public Order[] Orders { get; }
    }
}