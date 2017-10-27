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
        private readonly IMtMmRisksSlackNotificationsSender _slack;

        public AlertService(IRabbitMqService rabbitMqService,
            IReloadingManager<MarginTradingMarketMakerSettings> settings, IMtMmRisksSlackNotificationsSender slack)
        {
            _rabbitMqService = rabbitMqService;
            _settings = settings;
            _slack = slack;
        }

        public void AlertPrimaryExchangeSwitched(PrimaryExchangeSwitchedMessage message)
        {
            message.MarketMakerId = GetMarketMakerId();
            _rabbitMqService.GetProducer<PrimaryExchangeSwitchedMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.PrimaryExchangeSwitched), false)
                .ProduceAsync(message);
        }

        public void StopOrAllowNewTrades(string assetPairId, string reason, bool stop)
        {
            _rabbitMqService.GetProducer<StopOrAllowNewTradesMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.StopNewTrades), false)
                .ProduceAsync(new StopOrAllowNewTradesMessage
                {
                    AssetPairId = assetPairId,
                    MarketMakerId = GetMarketMakerId(),
                    Reason = reason,
                    Stop = stop
                });

            AlertRiskOfficer($"{(stop ? "Stop" : "Allow")}NewTrades for {assetPairId} because: {reason}");
        }

        public Task AlertRiskOfficer(string message)
        {
            Trace.Write($"{nameof(AlertRiskOfficer)}: {message}");
            return _slack.SendAsync(null, "MarketMaker", message);
        }

        public void AlertStarted()
        {
            AlertRiskOfficer("Market maker started");
            _rabbitMqService.GetProducer<StartedMessage>(
                    _settings.Nested(s => s.RabbitMq.Publishers.Started), true)
                .ProduceAsync(new StartedMessage {MarketMakerId = GetMarketMakerId()});
        }

        public Task AlertStopping()
        {
            return Task.WhenAll(
                AlertRiskOfficer("Market maker stopping"),
                _rabbitMqService.GetProducer<StoppingMessage>(
                        _settings.Nested(s => s.RabbitMq.Publishers.Stopping), true)
                    .ProduceAsync(new StoppingMessage {MarketMakerId = GetMarketMakerId()}));
        }

        private string GetMarketMakerId()
        {
            return _settings.CurrentValue.MarketMakerId;
        }
    }
}