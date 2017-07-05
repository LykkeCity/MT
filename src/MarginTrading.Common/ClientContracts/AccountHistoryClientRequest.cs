using System;
using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class AccountHistoryRpcClientRequest : AccountHistoryFiltersClientRequest
    {
        [Required]
        public string Token { get; set; }
    }

    public class AccountHistoryFiltersClientRequest
    {
        public string AccountId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool IsLive { get; set; }
    }
}
