using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class Db
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
        [AzureTableCheck]
        public string MarginTradingConnString { get; set; }
        [AzureTableCheck]
        public string HistoryConnString { get; set; }
        [AzureBlobCheck]
        public string StateConnString { get; set; }
        [AzureTableCheck]
        public string ReportsConnString { get; set; }
        [SqlCheck]
        public string ReportsSqlConnString { get; set; }
    }
}