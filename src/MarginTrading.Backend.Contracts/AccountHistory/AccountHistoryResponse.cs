namespace MarginTrading.Backend.Contracts.AccountHistory
{
    public class AccountHistoryResponse
    {
        public AccountHistoryContract[] Account { get; set; }
        public OrderHistoryContract[] PositionsHistory { get; set; }
        public OrderHistoryContract[] OpenPositions { get; set; }
    }
}