using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.ExternalOrderBroker.Models;

namespace MarginTrading.ExternalOrderBroker.Repositories.Azure
{
    internal class ExternalOrderReportAzureRepository : IExternalOrderReportRepository
    {
        private readonly INoSQLTableStorage<ExternalOrderReportEntity> _tableStorage;

        public ExternalOrderReportAzureRepository(IReloadingManager<Settings.AppSettings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<ExternalOrderReportEntity>.Create(settings.Nested(s => s.Db.ReportsConnString),
                "ExternalOrderReport", log);
        }

        public Task InsertOrReplaceAsync(IExternalOrderReport entity)
        {
            return _tableStorage.InsertOrReplaceAsync(ExternalOrderReportEntity.Create(entity));
        }
    }
}
