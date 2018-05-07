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
    public class OrderHistoryApplication : BrokerApplicationBase<OrderFullContract>
    {
        private readonly IOrdersHistoryRepository[] _ordersHistoryRepositories;
        private readonly Settings _settings;

        public OrderHistoryApplication(IOrdersHistoryRepository[] ordersHistoryRepositories, ILog logger,
            Settings settings, CurrentApplicationInfo applicationInfo,
            ISlackNotificationsSender slackNotificationsSender) : base(logger, slackNotificationsSender,
            applicationInfo)
        {
            _ordersHistoryRepositories = ordersHistoryRepositories;
            _settings = settings;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.OrderHistory.ExchangeName;

        protected override async Task HandleMessage(OrderFullContract order)
        {
            var orderHistory = order.ToOrderHistoryDomain();

            foreach (var ordersHistoryRepository in _ordersHistoryRepositories)
            {
                await ordersHistoryRepository.AddAsync(orderHistory);
            }
        }
    }
}