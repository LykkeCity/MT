using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class CloseOrderRequest : ClientRequest
    {
        [Required]
        public string OrderId { get; set; }
    }
}
