// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Extensions;

namespace MarginTrading.Backend.Services.Services
{
    /// <summary>
    /// Provides positions depending on request scope: either from current system state or from trading snapshot draft
    /// </summary>
    public class PositionsProvider : IPositionsProvider
    {
        private readonly OrdersCache _ordersCache;
        private readonly ILifetimeScope _lifetimeScope;

        public PositionsProvider(OrdersCache ordersCache, ILifetimeScope lifetimeScope)
        {
            _ordersCache = ordersCache;
            _lifetimeScope = lifetimeScope;
        }

        public ICollection<Position> GetPositionsByAccountIds(params string[] accountIds)
        {
            if (accountIds == null || !accountIds.Any() || accountIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentNullException(nameof(accountIds));
            
            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                if (scope.TryResolveSnapshotKeeper(out var snapshotKeeper))
                {
                    var positions = snapshotKeeper.GetPositions();

                    return positions
                        .Where(p => accountIds.Contains(p.AccountId))
                        .ToList();
                }
            }
            
            return _ordersCache.Positions.GetPositionsByAccountIds(accountIds);
        }
    }
}