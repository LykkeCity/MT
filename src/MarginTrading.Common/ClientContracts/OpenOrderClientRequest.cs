using System.ComponentModel.DataAnnotations;
using MarginTrading.Core;

namespace MarginTrading.Common.ClientContracts
{
    public class OpenOrderRpcClientRequest
    {
        [Required]
        public string Token { get; set; }
        public NewOrderClientContract Order { get; set; }
    }

    public class NewOrderClientContract
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
