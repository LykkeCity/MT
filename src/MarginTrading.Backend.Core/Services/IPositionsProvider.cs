// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Services
{
    public interface IPositionsProvider
    {
        ICollection<Position> GetPositionsByAccountIds(params string[] accountIds);
    }
}