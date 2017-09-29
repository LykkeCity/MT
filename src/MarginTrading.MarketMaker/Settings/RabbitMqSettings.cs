namespace MarginTrading.MarketMaker.Settings
{
    public class RabbitMqSettings
    {
        public RabbitConnectionSettings OrderCommandsConnectionSettings { get; set; }
        public RabbitConnectionSettings FiatOrderbooksConnectionSettings { get; set; }
        public RabbitConnectionSettings CryptoOrderbooksConnectionSettings { get; set; }
        public RabbitConnectionSettings SpotOrderBookConnectionSettings { get; set; }
    }
}