
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class ChangeOrderLimitRequest : ClientRequest
    {
        [Required]
        public string OrderId { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal StopLoss { get; set; }
        public decimal ExpectedOpenPrice { get; set; }
    }
}
