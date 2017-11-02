using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.OrderRejectedBroker
{
    public class Application : BrokerApplicationBase<OrderFullContract>
    {
        private readonly IMarginTradingOrdersRejectedRepository _ordersRejectedRepository;
        private readonly Settings _settings;

        public Application(IMarginTradingOrdersRejectedRepository ordersRejectedRepository,
            ILog logger, Settings settings, CurrentApplicationInfo applicationInfo,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _ordersRejectedRepository = ordersRejectedRepository;
            _settings = settings;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.OrderRejected.ExchangeName;

        protected override Task HandleMessage(OrderFullContract order)
        {
            var orderHistory = order.ToOrderHistoryDomain();
            return _ordersRejectedRepository.AddAsync(orderHistory);
        }
    }
}