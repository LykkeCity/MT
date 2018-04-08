using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountHistoryBroker.Repositories;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.BackendContracts;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker
{
    internal class Application : BrokerApplicationBase<AccountHistoryBackendContract>
    {
        private readonly IMarginTradingAccountHistoryRepository _accountHistoryRepository;
        private readonly IAccountTransactionsReportsRepository _accountTransactionsReportsRepository;
        private readonly Settings _settings;        

        public Application(IMarginTradingAccountHistoryRepository accountHistoryRepository, ILog logger,
            Settings settings, CurrentApplicationInfo applicationInfo,
            IAccountTransactionsReportsRepository accountTransactionsReportsRepository,
            ISlackNotificationsSender slackNotificationsSender)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _settings = settings;
            _accountTransactionsReportsRepository = accountTransactionsReportsRepository;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountHistory.ExchangeName;

        protected override Task HandleMessage(AccountHistoryBackendContract accountHistoryContract)
        {
            var accountHistory = accountHistoryContract.ToAccountHistoryContract();
            var accountTransactionReport = new AccountTransactionsReport
            {
                AccountId = accountHistoryContract.AccountId,
                ClientId = accountHistoryContract.ClientId,
                Comment = accountHistoryContract.Comment,
                Id = accountHistoryContract.Id,
                Amount = (double) accountHistoryContract.Amount,
                Balance = (double) accountHistoryContract.Balance,
                Date = accountHistoryContract.Date,
                Type = accountHistoryContract.Type.ToString(),
                WithdrawTransferLimit = (double) accountHistoryContract.WithdrawTransferLimit,
                PositionId = accountHistoryContract.OrderId,
                LegalEntity = accountHistory.LegalEntity,
                AuditLog = accountHistoryContract.AuditLog
            };
            
            return Task.WhenAll(
                _accountHistoryRepository.AddAsync(accountHistory),
                _accountTransactionsReportsRepository.InsertOrReplaceAsync(accountTransactionReport));
        }
    }
}