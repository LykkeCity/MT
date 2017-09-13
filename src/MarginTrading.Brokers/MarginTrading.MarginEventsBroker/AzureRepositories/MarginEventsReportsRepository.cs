using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.Core.Settings;

namespace MarginTrading.MarginEventsBroker.AzureRepositories
{
    internal class MarginEventsReportsRepository : IMarginEventsReportsRepository
    {
        private readonly INoSQLTableStorage<MarginEventReport> _tableStorage;

        public MarginEventsReportsRepository(MarginSettings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<MarginEventReport>.Create(() => settings.Db.ReportsConnString,
                "MarginTradingMarginEventsReports", log);
        }

        public Task InsertOrReplaceAsync(MarginEventReport entity)
        {
            return _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}
