namespace MarginTrading.Backend.Contracts.Orders
{
    public enum OrderTypeContract
    {
        Market = 1, // basic
        Limit = 2, // basic
        Stop = 3, // basic, closing only
        TakeProfit = 4, // related
        StopLoss = 5, // related
        TrailingStop = 6, // related
        //Closingout = 7,
        //Manual = 8
    }
}