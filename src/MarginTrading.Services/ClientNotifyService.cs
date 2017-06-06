using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using Microsoft.Extensions.PlatformAbstractions;

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
            string queueName = $"{_marginSettings.RabbitMqQueues.OrderChanged.ExchangeName}.{PlatformServices.Default.Application.ApplicationName}";
            _consoleWriter.WriteLine($"send order changed to queue {queueName}");
            _operationsLogService.AddLog($"queue {queueName}", order.ClientId, order.AccountId, null, order.ToJson());
        }

        public void NotifyAccountChanged(IMarginTradingAccount account)
        {
            _rabbitMqNotifyService.AccountChanged(account);
            string queueName = $"{_marginSettings.RabbitMqQueues.AccountChanged.ExchangeName}.{PlatformServices.Default.Application.ApplicationName}";
            _consoleWriter.WriteLine($"send account changed to queue {queueName}");
            _operationsLogService.AddLog($"queue {queueName}", account.ClientId, account.Id, null, account.ToJson());
        }

        public void NotifyAccountStopout(string clientId, string accountId, int positionsCount, double totalPnl)
        {
            _rabbitMqNotifyService.AccountStopout(clientId, accountId, positionsCount, totalPnl);
            string queueName = $"{_marginSettings.RabbitMqQueues.AccountStopout.ExchangeName}.{PlatformServices.Default.Application.ApplicationName}";
            _consoleWriter.WriteLine($"send account stopout to queue {queueName}");
            _operationsLogService.AddLog($"queue {queueName}", clientId, accountId, null,
                new {clientId = clientId, accountId = accountId, positionsCount = positionsCount, totalPnl = totalPnl}.ToJson());
        }
    }
}
