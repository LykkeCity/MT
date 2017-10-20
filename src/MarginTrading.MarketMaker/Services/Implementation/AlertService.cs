using System.Threading.Tasks;
using Common;
using Lykke.SettingsReader;
using Lykke.SlackNotifications;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class AlertService : IAlertService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IReloadingManager<MarginTradingMarketMakerSettings> _settings;
        private readonly ISlackNotificationsSender _slack;

        public AlertService(IRabbitMqService rabbitMqService,
            IReloadingManager<MarginTradingMarketMakerSettings> settings, ISlackNotificationsSender slack)
        {
            _rabbitMqService = rabbitMqService;
            _settings = settings;
            _slack = slack;
        }

        public void AlertPrimaryExchangeSwitched(PrimaryExchangeSwitchedMessage message)
        {
            message.MarketMakerId = GetMarketMakerId();
            Trace.Write($"{nameof(AlertPrimaryExchangeSwitched)}({message.ToJson()})");
            _rabbitMqService.GetProducer<PrimaryExchangeSwitchedMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.PrimaryExchangeSwitched), false)
                .ProduceAsync(message);
        }

        public void AlertStopNewTrades(string assetPairId, string reason)
        {
            _rabbitMqService.GetProducer<StopNewTradesMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.StopNewTrades), false)
                .ProduceAsync(new StopNewTradesMessage {AssetPairId = assetPairId, MarketMakerId = GetMarketMakerId(), Reason = reason });
            AlertRiskOfficer($"StopNewTrades was generated for {assetPairId} because: {reason}");
        }

        public void AlertRiskOfficer(string message)
        {
            Trace.Write($"{nameof(AlertRiskOfficer)}({message})");
            //_slack.SendAsync(null, "MarginTrading.MarketMaker.AlertService", message);
        }

        public void AlertStarted()
        {
            Trace.Write($"{nameof(AlertStarted)}()");
            _rabbitMqService.GetProducer<StartedMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.Started), true)
                .ProduceAsync(new StartedMessage {MarketMakerId = GetMarketMakerId()});
        }

        public Task AlertStopping()
        {
            Trace.Write($"{nameof(AlertStopping)}()");
            return _rabbitMqService.GetProducer<StoppingMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.Stopping), true)
                .ProduceAsync(new StoppingMessage {MarketMakerId = GetMarketMakerId()});
        }

        private string GetMarketMakerId()
        {
            return _settings.CurrentValue.MarketMakerId;
        }
    }
}