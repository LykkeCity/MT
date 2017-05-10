using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class ChangeOrderLimitsClientRequest
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string OrderId { get; set; }
        [Required]
        public string AccountId { get; set; }
        public double TakeProfit { get; set; }
        public double StopLoss { get; set; }
        public double ExpectedOpenPrice { get; set; }
    }
}
