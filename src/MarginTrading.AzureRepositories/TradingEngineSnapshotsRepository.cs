using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.AzureRepositories
{
    public class TradingEngineSnapshotsRepository : ITradingEngineSnapshotsRepository
    {
        public Task Add(DateTime tradingDay, string correlationId, DateTime timestamp, string orders, string positions,
            string accounts,
            string bestFxPrices, string bestTradingPrices)
        {
            throw new System.NotImplementedException();
        }
    }
}