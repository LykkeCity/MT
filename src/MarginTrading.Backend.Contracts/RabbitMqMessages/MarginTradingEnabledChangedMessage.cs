namespace MarginTrading.Backend.Contracts.RabbitMqMessages
{
    public class MarginTradingEnabledChangedMessage
    {
        public string ClientId { set; get; }
        public bool EnabledDemo { get; set; }
        public bool EnabledLive { get; set; }
    }
}