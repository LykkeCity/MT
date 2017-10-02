using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class ChangeOrderLimitsRpcClientRequest : ChangeOrderLimitsClientRequest
    {
        [Required]
        public string Token { get; set; }
    }

    public class ChangeOrderLimitsClientRequest
    {
        [Required]
        public string OrderId { get; set; }
        [Required]
        public string AccountId { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal StopLoss { get; set; }
        public decimal ExpectedOpenPrice { get; set; }
    }
}
