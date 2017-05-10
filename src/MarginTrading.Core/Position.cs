namespace MarginTrading.Core
{
    public class Position : IPosition
    {
        public string ClientId { get; set; }
        public string Asset { get; set; }
        public decimal Volume { get; set; }
    }
}
