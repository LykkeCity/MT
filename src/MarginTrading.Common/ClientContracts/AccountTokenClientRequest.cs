using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class AccountTokenClientRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string AccountId { get; set; }
    }
}
