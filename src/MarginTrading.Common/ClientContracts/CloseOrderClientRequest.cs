using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class CloseOrderClientRequest
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string AccountId { get; set; }
        [Required]
        public string OrderId { get; set; }
    }
}
