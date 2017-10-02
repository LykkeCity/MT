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
        public decimal? ExpectedOpenPrice { get; set; }
        [Required]
        public decimal? Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        [Required]
        public OrderFillType FillType { get; set; }
    }
}
