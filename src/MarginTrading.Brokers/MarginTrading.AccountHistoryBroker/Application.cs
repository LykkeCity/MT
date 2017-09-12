using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using MarginTrading.AccountHistoryBroker.AzureRepositories;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.AccountHistoryBroker
{
    internal class Application : BrokerApplicationBase<AccountHistoryBackendContract>
    {
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;
        private readonly IAccountTransactionsReportsRepository _accountTransactionsReportsRepository;
        private readonly MarginSettings _settings;

        public Application(IMarginTradingAccountHistoryRepository accountHistoryRepository, ILog logger,
            MarginSettings settings, CurrentApplicationInfo applicationInfo,
            IAccountTransactionsReportsRepository accountTransactionsReportsRepository)
            : base(logger, applicationInfo)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _settings = settings;
            _accountTransactionsReportsRepository = accountTransactionsReportsRepository;
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
            var accountTransactionReport = new AccountTransactionsReportsEntity
            {
                AccountId = accountHistoryContract.AccountId,
                ClientId = accountHistoryContract.ClientId,
                Comment = accountHistoryContract.Comment,
                Id = accountHistoryContract.Id,
                Amount = accountHistoryContract.Amount,
                Balance = accountHistoryContract.Balance,
                Date = accountHistoryContract.Date,
                Type = accountHistoryContract.Type.ToString(),
                WithdrawTransferLimit = accountHistoryContract.WithdrawTransferLimit,
            };
            _accountTransactionsReportsRepository.InsertOrReplaceAsync(accountTransactionReport);
            return Task.WhenAll(
                _accountHistoryRepository.AddAsync(accountHistory),
                _accountTransactionsReportsRepository.InsertOrReplaceAsync(accountTransactionReport));
        }
    }
}