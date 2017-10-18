using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Messages;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class AlertService : IAlertService
    {
        public void AlertPrimaryExchangeSwitched(PrimaryExchangeSwitchedMessage message)
        {
            // todo SendPrimaryExchangeSwitchedMessage
            // no alert to risk officer here
        }

        public void AlertStopNewTrades(string assetPairId)
        {
            // todo AlertStopNewTrades
            // also AlertRiskOfficer
        }

        public void AlertRiskOfficer(string message)
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
            throw new System.NotImplementedException();
        }
    }
}
