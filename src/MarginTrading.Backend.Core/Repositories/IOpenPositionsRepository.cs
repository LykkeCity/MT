// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IOpenPositionsRepository
    {
        Task Dump(IEnumerable<Position> openPositions);
    }
}