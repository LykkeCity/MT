
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class ChangeOrderLimitRequest : ClientRequest
    {
        [Required]
        public string OrderId { get; set; }
        public double TakeProfit { get; set; }
        public double StopLoss { get; set; }
        public double ExpectedOpenPrice { get; set; }
    }
}
