using System.ComponentModel.DataAnnotations;

namespace MarginTrading.Common.ClientContracts
{
    public class AddLimitOrdersClientRequest
    {
        [Required]
        public string Token { get; set; }
        public string MarketMakerId { get; set; }
        public bool DeleteAllBuy { get; set; }
        public bool DeleteAllSell { get; set; }
        public string[] DeleteByInstrumentsBuy { get; set; }
        public string[] DeleteByInstrumentsSell { get; set; }
        public LimitOrderClientContract[] OrdersToAdd { get; set; }
        public string[] OrderIdsToDelete { get; set; }
    }
}
