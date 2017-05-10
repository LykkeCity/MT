
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class AccountRequest : ClientRequest
    {
        [Required]
        public string AccountId { get; set; }
    }
}
