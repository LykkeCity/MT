using System.ComponentModel;

namespace MarginTrading.Contract.ClientContracts
{
    [DisplayName("Stopout info")]
    public class AccountStopoutClientContract
    {
        [DisplayName("Account id")]
        public string AccountId { get; set; }
        [DisplayName("Closed positions count")]
        public int PositionsCount { get; set; }
        [DisplayName("Total profit & loss")]
        public decimal TotalPnl { get; set; }
    }
}