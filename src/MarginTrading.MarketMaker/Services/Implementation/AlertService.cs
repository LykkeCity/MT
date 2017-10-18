using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.Messages;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class AlertService : IAlertService
    {
        private readonly IRabbitMqService _rabbitMqService;

        public AlertService(IRabbitMqService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService;
        }

        public void AlertPrimaryExchangeSwitched(PrimaryExchangeSwitchedMessage message)
        {
            //_rabbitMqService.GetProducer<PrimaryExchangeSwitchedMessage>()
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
