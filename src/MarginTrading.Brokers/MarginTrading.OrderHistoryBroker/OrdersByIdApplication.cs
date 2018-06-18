using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AzureRepositories.Snow.OrdersById;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.OrderHistoryBroker
{
    public class OrdersByIdApplication : BrokerApplicationBase<OrderFullContract>
    {
        private readonly IOrdersByIdRepository _ordersByIdRepository;
        private readonly Settings _settings;

        public OrdersByIdApplication(IOrdersByIdRepository ordersByIdRepository, ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo, ISlackNotificationsSender slackNotificationsSender) : base(logger,
            slackNotificationsSender, applicationInfo)
        {
            _ordersByIdRepository = ordersByIdRepository;
            _settings = settings;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.OrderHistory.ExchangeName;

        protected override Task HandleMessage(OrderFullContract order)
        {
            return _ordersByIdRepository.TryInsertAsync(new OrderById(order.Id, order.AccountId, order.CreateDate));
        }
    }
}