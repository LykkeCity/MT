using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class AccountHistoryBackendResponse
    {
        public AccountHistoryBackendContract[] Account { get; set; }
        public OrderHistoryBackendContract[] PositionsHistory { get; set; }
        public OrderHistoryBackendContract[] OpenPositions { get; set; }

        [Obsolete]
        public static AccountHistoryBackendResponse Create(IEnumerable<IMarginTradingAccountHistory> accounts, IEnumerable<Order> openPositions, IEnumerable<IOrderHistory> historyOrders)
        {
            return new AccountHistoryBackendResponse
            {
                Account = accounts.Select(item => item.ToBackendContract()).ToArray(),
                OpenPositions = openPositions.Select(item => item.ToBackendHistoryContract()).OrderByDescending(item => item.OpenDate).ToArray(),
                PositionsHistory = historyOrders.Select(item => item.ToBackendHistoryContract()).OrderByDescending(item => item.OpenDate).ToArray(),
            };
        }
    }
}
