using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Common.BackendContracts
{
    public class AddLimitOrdersBackendRequest
    {
        public string ClientId { get; set; }
        public string MarketMakerId { get; set; }
        public bool DeleteAllBuy { get; set; }
        public bool DeleteAllSell { get; set; }
        public string[] DeleteByInstrumentsBuy { get; set; }
        public string[] DeleteByInstrumentsSell { get; set; }
        public LimitOrderBackendContract[] OrdersToAdd { get; set; }
        public string[] OrderIdsToDelete { get; set; }

        public SetOrderModel GetModel()
        {
            return new SetOrderModel
            {
                MarketMakerId = MarketMakerId,
                DeleteAllBuy = DeleteAllBuy,
                DeleteAllSell = DeleteAllSell,
                DeleteByInstrumentsBuy = DeleteByInstrumentsBuy,
                DeleteByInstrumentsSell = DeleteByInstrumentsSell,
                OrdersToAdd = OrdersToAdd.Select(item => item.ToDomain()).ToArray(),
                OrderIdsToDelete = OrderIdsToDelete
            };
        }
    }
}
