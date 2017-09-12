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

namespace MarginTrading.AccountHistoryBroker
{
    public class Application : BrokerApplicationBase<AccountHistoryBackendContract>
    {
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;
        private readonly MarginSettings _settings;

        public Application(IMarginTradingAccountHistoryRepository accountHistoryRepository, ILog logger,
            MarginSettings settings, CurrentApplicationInfo applicationInfo) : base(logger, applicationInfo)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _settings = settings;
        }

        protected override RabbitMqSubscriptionSettings GetRabbitMqSubscriptionSettings()
        {
            return new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.MtRabbitMqConnString,
                QueueName =
                    QueueHelper.BuildQueueName(_settings.RabbitMqQueues.AccountHistory.ExchangeName, _settings.Env),
                ExchangeName = _settings.RabbitMqQueues.AccountHistory.ExchangeName,
                IsDurable = true
            };
        }

        protected override Task HandleMessage(AccountHistoryBackendContract accountHistoryContract)
        {
            var accountHistory = accountHistoryContract.ToAccountHistoryContract();
            return _accountHistoryRepository.AddAsync(accountHistory);
        }
    }
}