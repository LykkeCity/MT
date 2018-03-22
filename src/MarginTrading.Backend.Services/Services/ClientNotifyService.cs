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
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly MarginSettings _marginSettings;
        private readonly IConsole _consoleWriter;
        private readonly IAccountsCacheService _accountsCacheService;

        public ClientNotifyService(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IMarginTradingOperationsLogService operationsLogService,
            MarginSettings marginSettings,
            IConsole consoleWriter,
            IAccountsCacheService accountsCacheService)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _operationsLogService = operationsLogService;
            _marginSettings = marginSettings;
            _consoleWriter = consoleWriter;
            _accountsCacheService = accountsCacheService;
        }

        public void NotifyOrderChanged(Order order)
        {
            _rabbitMqNotifyService.OrderChanged(order);
            var queueName = QueueHelper.BuildQueueName(_marginSettings.RabbitMqQueues.OrderChanged.ExchangeName, _marginSettings.Env);
            _consoleWriter.WriteLine($"send order changed to queue {queueName}");
            _operationsLogService.AddLog($"queue {queueName}", order.ClientId, order.AccountId, null, order.ToJson());
        }

        public void NotifyAccountUpdated(IMarginTradingAccount account)
        {
            _rabbitMqNotifyService.AccountUpdated(account);
            var queueName = QueueHelper.BuildQueueName(_marginSettings.RabbitMqQueues.AccountChanged.ExchangeName, _marginSettings.Env);
            _consoleWriter.WriteLine($"send account changed to queue {queueName}");
            _operationsLogService.AddLog($"queue {queueName}", account.ClientId, account.Id, null, account.ToJson());
        }

        public void NotifyAccountStopout(string clientId, string accountId, int positionsCount, decimal totalPnl)
        {
            _rabbitMqNotifyService.AccountStopout(clientId, accountId, positionsCount, totalPnl);
            var queueName = QueueHelper.BuildQueueName(_marginSettings.RabbitMqQueues.AccountStopout.ExchangeName, _marginSettings.Env);
            _consoleWriter.WriteLine($"send account stopout to queue {queueName}");
            _operationsLogService.AddLog($"queue {queueName}", clientId, accountId, null,
                new {clientId = clientId, accountId = accountId, positionsCount = positionsCount, totalPnl = totalPnl}.ToJson());
        }
        
        public async Task NotifyTradingConditionsChanged(string tradingConditionId = null, string accountId = null)
        {
            if (!string.IsNullOrEmpty(tradingConditionId))
            {
                var clientIds = _accountsCacheService
                    .GetClientIdsByTradingConditionId(tradingConditionId, accountId).ToArray();

                if (clientIds.Length > 0)
                {
                    await _rabbitMqNotifyService.UserUpdates(true, false, clientIds);
                    _consoleWriter.WriteLine(
                        $"send user updates to queue {QueueHelper.BuildQueueName(_marginSettings.RabbitMqQueues.UserUpdates.ExchangeName, _marginSettings.Env)}");
                }
            }
        }
    }
}
