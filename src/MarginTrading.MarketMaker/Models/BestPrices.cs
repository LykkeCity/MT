namespace MarginTrading.MarketMaker.Models
{
    public struct BestPrices
    {
        public decimal BestBid { get; }
        public decimal BestAsk { get; }

        public BestPrices(decimal bestBid, decimal bestAsk)
        {
            BestBid = bestBid;
            BestAsk = bestAsk;
        }
    }
}