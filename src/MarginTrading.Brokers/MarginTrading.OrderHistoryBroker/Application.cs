using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.OrderHistoryBroker
{
    public class Application : BrokerApplicationBase<OrderFullContract>
    {
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly MarginSettings _settings;

        public Application(IMarginTradingOrdersHistoryRepository ordersHistoryRepository, ILog logger,
            MarginSettings settings, CurrentApplicationInfo applicationInfo) : base(logger, applicationInfo)
        {
            _ordersHistoryRepository = ordersHistoryRepository;
            _settings = settings;
        }

        protected override RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings()
        {
            var exchangeName = _settings.RabbitMqQueues.OrderHistory.ExchangeName;
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                QueueName = QueueHelper.BuildQueueName(exchangeName, _settings.Env),
                ExchangeName = exchangeName,
                IsDurable = true
            };
        }

        protected override Task HandleMessage(OrderFullContract order)
        {
            var orderHistory = order.ToOrderHistoryDomain();
            return _ordersHistoryRepository.AddAsync(orderHistory);
        }
    }
}