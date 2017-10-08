using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
    public interface IUpdatedAccountsTrackingService
    {
        /// <summary>
        /// Returns accounts which has been updated since the last call to this method
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<string> GetAccounts();
    }

    public class UpdatedAccountsTrackingService :
        IEventConsumer<AccountBalanceChangedEventArgs>,
        IUpdatedAccountsTrackingService
    {
        private readonly object _lock = new object();
        private Dictionary<string, string> _updatedAccounts = new Dictionary<string, string>();

        public IReadOnlyList<string> GetAccounts()
        {
            Dictionary<string, string> oldDict;
            lock (_lock)
            {
                oldDict = _updatedAccounts;
                _updatedAccounts = new Dictionary<string, string>();
            }

            return oldDict.Keys.ToList();
        }

        public void ConsumeEvent(object sender, AccountBalanceChangedEventArgs ea)
        {
            lock (_lock)
            {
                _updatedAccounts[ea.Account.Id] = ea.Account.Id;
            }
        }

        public int ConsumerRank => 101;
    }
}
