using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class AccountNewHistoryBackendResponse
    {
        public AccountHistoryItemBackend[] HistoryItems { get; set; }

        [Obsolete]
        public static AccountNewHistoryBackendResponse Create(IEnumerable<IMarginTradingAccountHistory> accounts, IEnumerable<IOrder> openOrders, IEnumerable<IOrderHistory> historyOrders)
        {
            var items = new List<AccountHistoryItemBackend>();
            var history = historyOrders.Where(item => item.OpenDate.HasValue).ToList();

            items.AddRange(accounts.Select(item => new AccountHistoryItemBackend { Account = item.ToBackendContract(), Date = item.Date }).ToList());

            items.AddRange(openOrders.Select(item => new AccountHistoryItemBackend { Position = item.ToBackendHistoryContract(), Date = item.OpenDate.Value }).ToList());

            items.AddRange(history.Select(item => new AccountHistoryItemBackend { Position = item.ToBackendHistoryOpenedContract(), Date = item.OpenDate.Value }).ToList());
            items.AddRange(history.Select(item => new AccountHistoryItemBackend { Position = item.ToBackendHistoryContract(), Date = item.CloseDate.Value }).ToList());

            items = items.OrderByDescending(item => item.Date).ToList();

            return new AccountNewHistoryBackendResponse
            {
                HistoryItems = items.ToArray()
            };
        }
    }
}
