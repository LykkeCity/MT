using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class AlertService : IAlertService
    {
        public void AlertPrimaryExchangeSwitched(string assetPairId, string newPrimaryExchange, ExchangeErrorState state, decimal preference)
        {
            // todo SendPrimaryExchangeSwitchedMessage
            // no alert to risk officer here
        }

        public void AlertStopNewTrades(string assetPairId)
        {
            // todo AlertStopNewTrades
            // also AlertRiskOfficer
        }

        public void AlertRiskOfficer(string message, object context)
        {
            // todo AlertRiskOfficer
        }

        public void AlertStarted()
        {
            throw new System.NotImplementedException();
            // send without wait
        }

        public Task AlertStopping()
        {
            
        }
    }
}
