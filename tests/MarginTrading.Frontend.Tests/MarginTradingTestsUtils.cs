using AzureStorage.Tables;
using MarginTrading.Frontend.Repositories;
using MarginTrading.Frontend.Repositories.Entities;

namespace MarginTrading.Frontend.Tests
{
    public class MarginTradingTestsUtils
    {
        public static MarginTradingWatchListsRepository GetPopulatedMarginTradingWatchListsRepository()
        {
            var repository = new MarginTradingWatchListsRepository(new NoSqlTableInMemory<MarginTradingWatchListEntity>());

            return repository;
        }
    }
}
