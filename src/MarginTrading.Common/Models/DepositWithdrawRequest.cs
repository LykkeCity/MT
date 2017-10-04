using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class DepositWithdrawRequest : ClientRequest
    {
        [Required]
        public string AccountId { get; set; }
        [Required]
        public decimal? Volume { get; set; }
    }
}
