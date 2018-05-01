using Lykke.SettingsReader.Attributes;

namespace MarginTrading.DataReader.Settings
{
    public class DataReaderSettings
    {
        public string ApiKey { get; set; }

        [Optional]
        public string Env { get; set; }

        public Db Db { get; set; }
    }
}