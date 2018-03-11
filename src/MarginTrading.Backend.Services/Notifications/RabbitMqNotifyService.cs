using System;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.Notifications
{
    public class RabbitMqNotifyService : IRabbitMqNotifyService
    {
        private readonly MarginSettings _settings;
        private readonly IIndex<string, IMessageProducer<string>> _publishers;
        private readonly ILog _log;

        public RabbitMqNotifyService(
            MarginSettings settings,
            IIndex<string, IMessageProducer<string>> publishers,
            ILog log)
        {
            _settings = settings;
            _publishers = publishers;
            _log = log;
        }

        public Task AccountHistory(string transactionId, string accountId, string clientId, decimal amount, decimal balance, decimal withdrawTransferLimit, AccountHistoryType type, string comment = null, string eventSourceId = null)
        {
            var record = new MarginTradingAccountHistory
            {
                Id = transactionId,
                AccountId = accountId,
                ClientId = clientId,
                Type = type,
                Amount = amount,
                Balance = balance,
                WithdrawTransferLimit = withdrawTransferLimit,
                Date = DateTime.UtcNow,
                Comment = comment,
                OrderId = type == AccountHistoryType.OrderClosed ? eventSourceId : null
            };

            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountHistory.ExchangeName, record.ToBackendContract());
        }

        public Task OrderHistory(IOrder order, OrderUpdateType orderUpdateType)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderHistory.ExchangeName, order.ToFullContract(orderUpdateType));
        }

        public Task OrderReject(IOrder order)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderRejected.ExchangeName, order.ToFullContract(OrderUpdateType.Reject));
        }

        public Task OrderBookPrice(InstrumentBidAskPair quote)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderbookPrices.ExchangeName, quote.ToRabbitMqContract());
        }

        public Task OrderChanged(IOrder order)
        {
            var message = order.ToBaseContract();
            return TryProduceMessageAsync(_settings.RabbitMqQueues.OrderChanged.ExchangeName, message);
        }

        public Task AccountUpdated(IMarginTradingAccount account)
        {
            return AccountChanged(account, AccountEventTypeEnum.Updated);
        }

        public Task AccountDeleted(IMarginTradingAccount account)
        {
            return AccountChanged(account, AccountEventTypeEnum.Deleted);
        }

        public Task AccountCreated(IMarginTradingAccount account)
        {
            return AccountChanged(account, AccountEventTypeEnum.Created);
        }

        private Task AccountChanged(IMarginTradingAccount account, AccountEventTypeEnum eventType)
        {
            var message = new AccountChangedMessage
            {
                Account = account.ToFullBackendContract(_settings.IsLive),
                EventType = eventType,
            };

            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountChanged.ExchangeName, message);
        }

        public Task AccountMarginEvent(AccountMarginEventMessage eventMessage)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName, eventMessage);
        }

        public Task AccountStopout(string clientId, string accountId, int positionsCount, decimal totalPnl)
        {
            var message = new { clientId, accountId, positionsCount, totalPnl };
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountStopout.ExchangeName, message);
        }

        public Task UserUpdates(bool updateAccountAssets, bool updateAccounts, string[] clientIds)
        {
            var message = new { updateAccountAssetPairs = updateAccountAssets, UpdateAccounts = updateAccounts, clientIds };
            return TryProduceMessageAsync(_settings.RabbitMqQueues.UserUpdates.ExchangeName, message);
        }

        public Task UpdateAccountStats(AccountStatsUpdateMessage message)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountStats.ExchangeName, message);
        }

        public Task NewTrade(TradeContract trade)
        {
            return TryProduceMessageAsync(_settings.RabbitMqQueues.Trades.ExchangeName, trade);
        }

        private async Task TryProduceMessageAsync(string exchangeName, object message)
        {
            string messageStr = null;
            try
            {
                messageStr = message.ToJson();
                await _publishers[exchangeName].ProduceAsync(messageStr);
            }
            catch (Exception ex)
            {
#pragma warning disable 4014
                _log.WriteErrorAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr, ex);
#pragma warning restore 4014
            }
        }

        public void Stop()
        {
            ((IStopable)_publishers[_settings.RabbitMqQueues.AccountHistory.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.OrderHistory.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.OrderRejected.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.OrderbookPrices.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.AccountStopout.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.AccountChanged.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.UserUpdates.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.AccountStats.ExchangeName]).Stop();
            ((IStopable)_publishers[_settings.RabbitMqQueues.Trades.ExchangeName]).Stop();
        }
    }
}
