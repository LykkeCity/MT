using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using MarginTrading.Common.Services;
using MarginTrading.Frontend.Repositories.Contract;
using MarginTrading.Frontend.Repositories.Entities;

namespace MarginTrading.Frontend.Repositories
{
    public class MaintenanceInfoRepository : IMaintenanceInfoRepository
    {
        private readonly INoSQLTableStorage<MaintenanceInfoEntity> _tableStorage;

        public MaintenanceInfoRepository(IReloadingManager<string> connectionStringManager)
        {
            _tableStorage = AzureTableStorage<MaintenanceInfoEntity>.Create(connectionStringManager,
                "MaintenanceInfo", LogLocator.CommonLog);
        }

        public async Task<IMaintenanceInfo> GetMaintenanceInfo(bool isLive)
        {
            var rk = isLive ? MaintenanceInfoEntity.GetLiveRowKey() : MaintenanceInfoEntity.GetDemoRowKey();

            return (IMaintenanceInfo) await _tableStorage.GetDataAsync(MaintenanceInfoEntity.GetPartitionKey(), rk) ??
                   new MaintenanceInfo();
        }
    }
}