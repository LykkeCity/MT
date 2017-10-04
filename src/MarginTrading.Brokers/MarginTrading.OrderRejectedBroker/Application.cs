using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.OrderRejectedBroker
{
    public class Application : BrokerApplicationBase<OrderFullContract>
    {
        private readonly IMarginTradingOrdersRejectedRepository _ordersRejectedRepository;
        private readonly MarginSettings _settings;

        public Application(IMarginTradingOrdersRejectedRepository ordersRejectedRepository,
            ILog logger, MarginSettings settings, CurrentApplicationInfo applicationInfo)
            : base(logger, applicationInfo)
        {
            _ordersRejectedRepository = ordersRejectedRepository;
            _settings = settings;
        }

        protected override RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings()
        {
            var exchangeName = _settings.RabbitMqQueues.OrderRejected.ExchangeName;
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
            return _ordersRejectedRepository.AddAsync(orderHistory);
        }
    }
}