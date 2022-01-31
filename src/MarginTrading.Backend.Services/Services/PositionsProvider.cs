// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Log;
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
        private readonly IDraftSnapshotKeeper _draftSnapshotKeeper;
        private readonly ILog _log;

        public PositionsProvider(OrdersCache ordersCache, IDraftSnapshotKeeper draftSnapshotKeeper, ILog log)
        {
            _ordersCache = ordersCache;
            _draftSnapshotKeeper = draftSnapshotKeeper;
            _log = log;
        }

        public ICollection<Position> GetPositionsByAccountIds(params string[] accountIds)
        {
            if (accountIds == null || !accountIds.Any() || accountIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentNullException(nameof(accountIds));

            if (_draftSnapshotKeeper.Initialized())
            {
                _log.WriteInfoAsync(nameof(PositionsProvider),
                    nameof(GetPositionsByAccountIds),
                    _draftSnapshotKeeper.TradingDay.ToJson(),
                    "Draft snapshot keeper initialized and will be used as positions provider");
                
                var positions = _draftSnapshotKeeper.GetPositions();

                return positions
                    .Where(p => accountIds.Contains(p.AccountId))
                    .ToList();
            }
            
            return _ordersCache.Positions.GetPositionsByAccountIds(accountIds);
        }
    }
}