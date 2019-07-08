// Copyright (c) 2019 Lykke Corp.

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
