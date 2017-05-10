using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class SetActiveAccountClientRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string AccountId { get; set; }
    }
}
