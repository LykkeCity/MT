using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class DepositWithdrawClientRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string AccountId { get; set; }
        [Required]
        public decimal? Volume { get; set; }
    }
}
