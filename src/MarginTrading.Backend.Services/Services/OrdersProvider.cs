// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Services
{
    /// <summary>
    /// Provides orders depending on request scope: either from current system state or from trading snapshot draft
    /// </summary>
    public class OrdersProvider : IOrdersProvider
    {
        private readonly OrdersCache _ordersCache;
        private readonly ILifetimeScope _lifetimeScope;

        public OrdersProvider(OrdersCache ordersCache, ILifetimeScope lifetimeScope)
        {
            _ordersCache = ordersCache;
            _lifetimeScope = lifetimeScope;
        }

        public ICollection<Order> GetActiveOrdersByAccountIds(params string[] accountIds)
        {
            if (accountIds == null || !accountIds.Any() || accountIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentNullException(nameof(accountIds));
            
            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                if (scope.TryResolve<IDraftSnapshotKeeper>(out var snapshotKeeper))
                {
                    var orders = snapshotKeeper.GetAllOrders();

                    return orders
                        .Where(o => o.Status == OrderStatus.Active && accountIds.Contains(o.AccountId))
                        .ToList();
                }
            }
            
            return _ordersCache.Active.GetOrdersByAccountIds(accountIds);
        }
    }
}