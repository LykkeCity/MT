using System;
using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class AccountHistoryClientRequest
    {
        [Required]
        public string Token { get; set; }
        public string AccountId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool IsLive { get; set; }
    }
}
