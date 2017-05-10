using System.ComponentModel.DataAnnotations;
using MarginTrading.Core;

namespace MarginTrading.Common.Models
{
    public class AddOrdersRequest : ClientRequest
    {
        [Required]
        public string MarketMakerId { get; set; }
        public LimitOrder[] OrdersToAdd { get; set; }
        public string[] OrderIdsToDelete { get; set; }
        public bool DeleteAll { get; set; }
    }
}
