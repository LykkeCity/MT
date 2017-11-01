using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.OrderHistoryBroker
{
    public class Application : BrokerApplicationBase<OrderFullContract>
    {
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly Settings _settings;

        public Application(IMarginTradingOrdersHistoryRepository ordersHistoryRepository, ILog logger,
            Settings settings, CurrentApplicationInfo applicationInfo,
            ISlackNotificationsSender slackNotificationsSender) : base(logger, slackNotificationsSender,
            applicationInfo)
        {
            _ordersHistoryRepository = ordersHistoryRepository;
            _settings = settings;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.OrderHistory.ExchangeName;

        protected override Task HandleMessage(OrderFullContract order)
        {
            var orderHistory = order.ToOrderHistoryDomain();
            return _ordersHistoryRepository.AddAsync(orderHistory);
        }
    }
}