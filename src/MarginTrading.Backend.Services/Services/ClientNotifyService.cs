using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    public class ClientNotifyService : IClientNotifyService
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IOperationsLogService _operationsLogService;
        private readonly MarginTradingSettings _marginSettings;
        private readonly IConsole _consoleWriter;
        private readonly IAccountsCacheService _accountsCacheService;

        public ClientNotifyService(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IOperationsLogService operationsLogService,
            MarginTradingSettings marginSettings,
            IConsole consoleWriter,
            IAccountsCacheService accountsCacheService)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _operationsLogService = operationsLogService;
            _marginSettings = marginSettings;
            _consoleWriter = consoleWriter;
            _accountsCacheService = accountsCacheService;
        }

        public void NotifyAccountUpdated(IMarginTradingAccount account)
        {
            _rabbitMqNotifyService.AccountUpdated(account);
            var queueName = QueueHelper.BuildQueueName(_marginSettings.RabbitMqQueues.AccountChanged.ExchangeName, _marginSettings.Env);
            _operationsLogService.AddLog($"queue {queueName}", account.Id, null, account.ToJson());
        }
    }
}
