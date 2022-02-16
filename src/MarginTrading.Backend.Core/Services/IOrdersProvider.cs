// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Services
{
    public interface IOrdersProvider
    {
        ICollection<Order> GetActiveOrdersByAccountIds(params string[] accountIds);
    }
}