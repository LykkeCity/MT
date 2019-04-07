using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class Db
    {
        public StorageMode StorageMode { get; set; }

        [Optional]
        public string LogsConnString { get; set; }
        
        public string MarginTradingConnString { get; set; }

        public string StateConnString { get; set; }
        
        public string SqlConnectionString { get; set; }
        
        public string OrdersHistorySqlConnectionString { get; set; }
        
        public string OrdersHistoryTableName { get; set; }
        
        public string PositionsHistorySqlConnectionString { get; set; }
        
        public string PositionsHistoryTableName { get; set; }
    }
}