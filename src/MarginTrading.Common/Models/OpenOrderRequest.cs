
using System.ComponentModel.DataAnnotations;
using MarginTrading.Core;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class OpenOrderRequest : ClientRequest
    {
        public NewOrder Order { get; set; }
    }

    public class NewOrder
    {
        [Required]
        public string AccountId { get; set; }
        [Required]
        public string Instrument { get; set; }
        public double? ExpectedOpenPrice { get; set; }
        [Required]
        public double? Volume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        [Required]
        public OrderFillType FillType { get; set; }
    }
}
