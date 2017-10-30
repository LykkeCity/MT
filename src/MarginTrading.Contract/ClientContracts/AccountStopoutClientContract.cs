namespace MarginTrading.Contract.ClientContracts
{
    public class AccountStopoutClientContract
    {
        public string AccountId { get; set; }
        public int PositionsCount { get; set; }
        public decimal TotalPnl { get; set; }
    }
}