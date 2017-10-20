namespace MarginTrading.MarketMaker.Settings
{
    public class RabbitMqPublishersSettings
    {
        public RabbitConnectionSettings OrderCommands { get; set; }
        public RabbitConnectionSettings PrimaryExchangeSwitched { get; set; }
        public RabbitConnectionSettings StopNewTrades { get; set; }
        public RabbitConnectionSettings Started { get; set; }
        public RabbitConnectionSettings Stopping { get; set; }
    }
}
