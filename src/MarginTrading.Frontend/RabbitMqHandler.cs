using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Frontend.Settings;
using WampSharp.V2.Realm;

namespace MarginTrading.Frontend
{
    public class RabbitMqHandler
    {
        private readonly IWampHostedRealm _realm;
        private readonly IClientNotificationService _clientNotificationService;
        private readonly IMarginTradingOperationsLogService _operationsLog;
        private readonly MtFrontendSettings _settings;
        private readonly IConsole _consoleWriter;
        private readonly ILog _log;
        private readonly ISubject<InstrumentBidAskPair> _subject;

        public RabbitMqHandler(
            IWampHostedRealm realm,
            IClientNotificationService clientNotificationService,
            IMarginTradingOperationsLogService operationsLogService,
            MtFrontendSettings settings,
            MtFrontSettings frontSettings,
            IConsole consoleWriter,
            ILog log)
        {
            _realm = realm;
            _clientNotificationService = clientNotificationService;
            _operationsLog = operationsLogService;
            _settings = settings;
            _consoleWriter = consoleWriter;
            _log = log;
            _subject = realm.Services.GetSubject<InstrumentBidAskPair>(frontSettings.WampPricesTopicName);
        }

        public async Task ProcessPrices(InstrumentBidAskPair bidAskPair)
        {
            _subject.OnNext(bidAskPair);
            await Task.FromResult(0);
        }

        public async Task ProcessAccountChanged(MarginTradingAccountBackendContract account)
        {
            _consoleWriter.WriteLine($"Get account change from {_settings.MarginTradingFront.RabbitMqQueues.AccountChanged.QueueName} queue for clientId = {account.ClientId}");
            string notificationId = await _clientNotificationService.GetNotificationId(account.ClientId);
            var userTopic = _realm.Services.GetSubject<NotifyResponse<MarginTradingAccountClientContract>>($"user.{notificationId}");

            var notifyResponse = new NotifyResponse<MarginTradingAccountClientContract>
            {
                Entity = account.ToClientContract(),
                Type = NotifyEntityType.Account
            };

            userTopic.OnNext(notifyResponse);

            _operationsLog.AddLog($"topic user.{notificationId} (account changed)", account.ClientId, account.Id, null, notifyResponse.ToJson());
            _consoleWriter.WriteLine($"topic user.{notificationId} (account changed) for clientId = {account.ClientId}");

            var userUpdateTopic = _realm.Services.GetSubject<NotifyResponse>($"user.updates.{notificationId}");
            var userUpdateTopicResponse = new NotifyResponse { Account = notifyResponse.Entity, Order = null };
            
            userUpdateTopic.OnNext(userUpdateTopicResponse);

            _operationsLog.AddLog($"topic user.updates.{notificationId} (account changed)", account.ClientId,
                account.Id, null, userUpdateTopicResponse.ToJson());
            _consoleWriter.WriteLine($"topic user.updates.{notificationId} (account changed) for clientId = {account.ClientId}");
        }

        public async Task ProcessOrderChanged(OrderContract order)
        {
            _consoleWriter.WriteLine($"Get order change from {_settings.MarginTradingFront.RabbitMqQueues.OrderChanged.QueueName} queue for clientId = {order.ClientId}");

            string notificationId = await _clientNotificationService.GetNotificationId(order.ClientId);
            var userTopic = _realm.Services.GetSubject<NotifyResponse<OrderClientContract>>($"user.{notificationId}");

            var notifyResponse = new NotifyResponse<OrderClientContract>
            {
                Entity = order.ToClientContract(),
                Type = NotifyEntityType.Order
            };

            userTopic.OnNext(notifyResponse);

            _operationsLog.AddLog($"topic user.{notificationId} (position changed)", order.ClientId, order.AccountId, null, notifyResponse.ToJson());
            _consoleWriter.WriteLine($"topic user.{notificationId} (order changed) for clientId = {order.ClientId}");

            var userUpdateTopic = _realm.Services.GetSubject<NotifyResponse>($"user.updates.{notificationId}");
            var userUpdateTopicResponse = new NotifyResponse { Account = null, Order = notifyResponse.Entity };
            
            userUpdateTopic.OnNext(userUpdateTopicResponse);

            _operationsLog.AddLog($"topic user.updates.{notificationId} (position changed)", order.ClientId, notifyResponse.Entity.AccountId, null, userUpdateTopicResponse.ToJson());
            _consoleWriter.WriteLine($"topic user.updates.{notificationId} (order changed) for clientId = {order.ClientId}");
        }

        public async Task ProcessAccountStopout(AccountStopoutBackendContract stopout)
        {
            _consoleWriter.WriteLine($"Get account stopout from {_settings.MarginTradingFront.RabbitMqQueues.AccountStopout.QueueName} queue for clientId = {stopout.ClientId}");

            string notificationId = await _clientNotificationService.GetNotificationId(stopout.ClientId);
            var userTopic = _realm.Services.GetSubject<NotifyResponse<AccountStopoutClientContract>>($"user.{notificationId}");

            var response = new NotifyResponse<AccountStopoutClientContract>
            {
                Entity = stopout.ToClientContract(),
                Type = NotifyEntityType.AccountStopout
            };

            userTopic.OnNext(response);

            _operationsLog.AddLog($"topic user.{notificationId} (account stopout)", stopout.ClientId, response.Entity.AccountId, null, response.ToJson());
            _consoleWriter.WriteLine($"topic user.{notificationId} (account stopout) for clientId = {stopout.ClientId}");

            var userUpdateTopic = _realm.Services.GetSubject<NotifyResponse>($"user.updates.{notificationId}");
            var userUpdateTopicResponse = new NotifyResponse { Account = null, Order = null, AccountStopout = response.Entity };
            
            userUpdateTopic.OnNext(userUpdateTopicResponse);

            _operationsLog.AddLog($"topic user.updates.{notificationId} (account stopout)", stopout.ClientId, response.Entity.AccountId, null, userUpdateTopicResponse.ToJson());
            _consoleWriter.WriteLine($"topic user.updates.{notificationId} (account stopout) for clientId = {stopout.ClientId}");
        }

        public async Task ProcessUserUpdates(UserUpdateEntityBackendContract userUpdate)
        {
            _consoleWriter.WriteLine($"Get user update from {_settings.MarginTradingFront.RabbitMqQueues.UserUpdates.QueueName} queue for {userUpdate.ClientIds.Length} clients");

            foreach (var clientId in userUpdate.ClientIds)
            {
                try
                {
                    string notificationId = await _clientNotificationService.GetNotificationId(clientId);
                    var userTopic =
                        _realm.Services.GetSubject<NotifyResponse<UserUpdateEntityClientContract>>(
                            $"user.{notificationId}");

                    var response = new NotifyResponse<UserUpdateEntityClientContract>
                    {
                        Entity = userUpdate.ToClientContract(),
                        Type = NotifyEntityType.UserUpdate
                    };

                    userTopic.OnNext(response);

                    string eventType = string.Empty;

                    if (userUpdate.UpdateAccountAssetPairs)
                    {
                        eventType = "account assets";
                    }

                    if (userUpdate.UpdateAccounts)
                    {
                        eventType = "accounts";
                    }

                    _operationsLog.AddLog($"topic user.{notificationId} ({eventType} changed)", clientId, null, null,
                        response.ToJson());
                    _consoleWriter.WriteLine(
                        $"topic user.{notificationId} ({eventType} changed) for clientId = {clientId}");

                    var userUpdateTopic = _realm.Services.GetSubject<NotifyResponse>($"user.updates.{notificationId}");
                    var userUpdateTopicResponse = new NotifyResponse {UserUpdate = response.Entity};

                    userUpdateTopic.OnNext(userUpdateTopicResponse);

                    _operationsLog.AddLog($"topic user.updates.{notificationId} ({eventType} changed)", clientId, null,
                        null, userUpdateTopicResponse.ToJson());
                    _consoleWriter.WriteLine(
                        $"topic user.updates.{notificationId} (account assets changed) for clientId = {clientId}");
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(RabbitMqHandler), nameof(ProcessUserUpdates), clientId, ex);
                }
            }
        }
    }
}
