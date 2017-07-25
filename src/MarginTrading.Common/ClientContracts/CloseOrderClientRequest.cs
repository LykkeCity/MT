using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class CloseOrderRpcClientRequest : CloseOrderClientRequest
    {
        [Required]
        public string Token { get; set; }
    }

    public class CloseOrderClientRequest
    {
        [Required]
        public string AccountId { get; set; }
        [Required]
        public string OrderId { get; set; }
    }
}
