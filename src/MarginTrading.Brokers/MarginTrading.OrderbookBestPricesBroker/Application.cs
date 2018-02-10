using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;
using MarginTrading.OrderbookBestPricesBroker.Repositories;

namespace MarginTrading.OrderbookBestPricesBroker
{
    internal class Application : BrokerApplicationBase<BidAskPairRabbitMqContract>
    {
        private readonly IOrderbookBestPricesRepository _orderbookBestPricesRepository;
        private readonly Settings _settings;

        public Application(ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo,
            IOrderbookBestPricesRepository orderbookBestPricesRepository,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _settings = settings;
            _orderbookBestPricesRepository = orderbookBestPricesRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.OrderbookPrices.ExchangeName;

        protected override Task HandleMessage(BidAskPairRabbitMqContract message)
        {
            return _orderbookBestPricesRepository.InsertOrReplaceAsync(new OrderbookBestPricesEntity
            {
                AssetPairId = message.Instrument,
                Bid = message.Bid,
                Ask = message.Ask
            }, message.Date);
        }
    }
}