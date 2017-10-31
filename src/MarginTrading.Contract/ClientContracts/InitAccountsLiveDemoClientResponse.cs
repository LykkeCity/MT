namespace MarginTrading.Contract.ClientContracts
{
    public class InitAccountsLiveDemoClientResponse
    {
        public MarginTradingAccountClientContract[] Live { get; set; }
        public MarginTradingAccountClientContract[] Demo { get; set; }
    }
}