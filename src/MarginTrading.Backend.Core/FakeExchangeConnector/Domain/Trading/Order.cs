namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading
{
    public class Order
    {
        public Order(string instrument, OrderType type, TradingSignal signal)
        {
            Instrument = instrument;
            Type = type;
            Signal = signal;
        }

        public string Instrument { get; }
        
//        private decimal stopLoss;
//        private decimal takeProfit;
        
        public OrderType Type { get; }

        public TradingSignal Signal { get; }
    }
}
