using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.MarketMaker.Settings
{
    public class DbSettings
    {
        public string ConnectionString { get; set; }
        public string LogsConnString { get; set; }
        [Optional, CanBeNull]
        public string QueuePersistanceRepositoryConnString { get; set; }
    }
}
