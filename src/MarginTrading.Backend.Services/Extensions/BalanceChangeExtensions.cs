// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MarginTrading.AccountsManagement.Contracts.Models;

namespace MarginTrading.Backend.Services.Extensions
{
    public static class BalanceChangeExtensions
    {
        public static decimal GetTotalByType(this IEnumerable<AccountBalanceChangeLightContract> items, AccountBalanceChangeReasonTypeContract type)
        {
            if (items == null || !items.Any())
                return 0;

            return items
                .Where(x => x.ReasonType == type)
                .Sum(x => x.ChangeAmount);
        }
    }
}