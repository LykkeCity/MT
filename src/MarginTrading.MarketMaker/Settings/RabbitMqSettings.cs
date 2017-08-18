namespace MarginTrading.MarketMaker.Settings
{
    public class RabbitMqSettings
    {
        public RabbitConnectionSettings OrderCommandsConnectionSettings { get; set; }
        public RabbitConnectionSettings IcmQuotesConnectionSettings { get; set; }
        public RabbitConnectionSettings SpotOrderBookConnectionSettings { get; set; }
    }
}