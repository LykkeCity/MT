using System;
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class AccountHistoryRequest : ClientRequest
    {
        public string AccountId { get; set; }
        [Required]
        public DateTime? From { get; set; }
        [Required]
        public DateTime? To { get; set; }
    }
}
