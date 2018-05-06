using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AzureRepositories.Snow.Trades;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.OrderHistoryBroker
{
    public class TradesApplication : BrokerApplicationBase<TradeContract>
    {
        private readonly ITradesRepository _tradesRepository;
        private readonly Settings _settings;

        public TradesApplication(ITradesRepository tradesRepository, ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo, ISlackNotificationsSender slackNotificationsSender) : base(logger,
            slackNotificationsSender, applicationInfo)
        {
            _tradesRepository = tradesRepository;
            _settings = settings;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.Trades.ExchangeName;
        protected override string QueuePostfix => ".Trades";

        protected override Task HandleMessage(TradeContract trade)
        {
            return _tradesRepository.UpsertAsync(new TradeEntity
            {
                Id = trade.Id,
                OrderId = trade.OrderId,
                PositionId = trade.OrderId,
                AccountId = trade.AccountId,
                AssetPairId = trade.AssetPairId,
                ClientId = trade.ClientId, //todo: remove client id?
                Price = trade.Price,
                Volume = trade.Volume,
                TradeTimestamp = trade.Date,
                Type = trade.Type,
            });
        }
    }
}