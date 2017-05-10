using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class ClientNotifyService : IClientNotifyService
    {
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly MarginSettings _marginSettings;
        private readonly IConsole _consoleWriter;

        public ClientNotifyService(
            IRabbitMqNotifyService rabbitMqNotifyService,
            IMarginTradingOperationsLogService operationsLogService,
            MarginSettings marginSettings,
            IConsole consoleWriter)
        {
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _operationsLogService = operationsLogService;
            _marginSettings = marginSettings;
            _consoleWriter = consoleWriter;
        }

        public void NotifyOrderChanged(Order order)
        {
            _rabbitMqNotifyService.OrderChanged(order);
            _consoleWriter.WriteLine($"send order changed to queue {_marginSettings.RabbitMqQueues.OrderChanged.QueueName}");
            _operationsLogService.AddLog($"queue {_marginSettings.RabbitMqQueues.OrderChanged.QueueName}", order.ClientId, order.AccountId, null, order.ToJson());
        }

        public void NotifyAccountChanged(IMarginTradingAccount account)
        {
            _rabbitMqNotifyService.AccountChanged(account);
            _consoleWriter.WriteLine($"send account changed to queue {_marginSettings.RabbitMqQueues.AccountChanged.QueueName}");
            _operationsLogService.AddLog($"queue {_marginSettings.RabbitMqQueues.AccountChanged.QueueName}", account.ClientId, account.Id, null, account.ToJson());
        }

        public void NotifyAccountStopout(string clientId, string accountId, int positionsCount, double totalPnl)
        {
            _rabbitMqNotifyService.AccountStopout(clientId, accountId, positionsCount, totalPnl);
            _consoleWriter.WriteLine($"send account stopout to queue {_marginSettings.RabbitMqQueues.AccountStopout.QueueName}");
            _operationsLogService.AddLog($"queue {_marginSettings.RabbitMqQueues.AccountStopout.QueueName}", clientId, accountId, null,
                new {clientId = clientId, accountId = accountId, positionsCount = positionsCount, totalPnl = totalPnl}.ToJson());
        }
    }
}
