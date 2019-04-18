using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface ITradingEngineSnapshotsRepository
    {
        Task Add(string correlationId, DateTime timestamp, IEnumerable<OrderContract> orders,
            IEnumerable<OpenPositionContract> positions, IEnumerable<AccountStatContract> accounts,
            Dictionary<string, BestPriceContract> bestFxPrices,
            Dictionary<string, BestPriceContract> bestTradingPrices);
    }
}