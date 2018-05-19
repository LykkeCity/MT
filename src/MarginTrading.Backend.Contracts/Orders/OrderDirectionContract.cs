namespace MarginTrading.Backend.Contracts.Orders
{
    /// <summary>
    /// The direction of an order
    /// </summary>
    public enum OrderDirectionContract
    {
        /// <summary>
        /// Order to buy the quoting asset of a pair
        /// </summary>
        Buy = 1,
        
        /// <summary>
        /// Order to sell the quoting asset of a pair
        /// </summary>
        Sell = 2
    }}