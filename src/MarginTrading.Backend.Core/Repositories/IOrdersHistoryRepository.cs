// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IOrdersHistoryRepository
    {
        Task<IReadOnlyList<IOrderHistory>> GetLastSnapshot(DateTime @from);
    }
}
