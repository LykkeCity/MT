
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
        public decimal? ExpectedOpenPrice { get; set; }
        [Required]
        public decimal? Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        [Required]
        public OrderFillType FillType { get; set; }
    }
}
