namespace MarginTrading.BrokerBase.Settings
{
    public class BrokerSettingsBase
    {
        public string MtRabbitMqConnString { get; set; }
        public bool IsLive { get; set; }
        public string Env { get; set; }
    }
}