using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.Events
{
    public class StopOutEventArgs
    {
        public StopOutEventArgs(MarginTradingAccount account, Position[] orders)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (orders == null) throw new ArgumentNullException(nameof(orders));
            Account = account;
            Orders = orders;
        }

        public MarginTradingAccount Account { get; }
        public Position[] Orders { get; }
    }
}