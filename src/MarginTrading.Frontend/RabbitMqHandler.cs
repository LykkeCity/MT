using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.RabbitMqMessageModels;
using MarginTrading.Core;
using MarginTrading.Core.Enums;
using MarginTrading.Frontend.Settings;
using WampSharp.V2.Realm;

namespace MarginTrading.Frontend
{
    public class RabbitMqHandler
    {
        private readonly IWampHostedRealm _realm;
        private readonly IClientAccountService _clientNotificationService;
        private readonly IMarginTradingOperationsLogService _operationsLog;
        private readonly MtFrontendSettings _settings;
        private readonly MtFrontSettings _frontSettings;
        private readonly IConsole _consoleWriter;
        private readonly ILog _log;
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;
        private readonly ISubject<InstrumentBidAskPair> _allPairsSubject;

        private readonly ConcurrentDictionary<string, ISubject<InstrumentBidAskPair>> _priceSubjects =
            new ConcurrentDictionary<string, ISubject<InstrumentBidAskPair>>();

        public RabbitMqHandler(
            IWampHostedRealm realm,
            IClientAccountService clientNotificationService,
            IMarginTradingOperationsLogService operationsLogService,
            MtFrontendSettings settings,
            MtFrontSettings frontSettings,
            IConsole consoleWriter,
            ILog log,
            IMarginTradingSettingsService marginTradingSettingsService)
        {
            _realm = realm;
            _clientNotificationService = clientNotificationService;
            _operationsLog = operationsLogService;
            _settings = settings;
            _frontSettings = frontSettings;
            _consoleWriter = consoleWriter;
            _log = log;
            _marginTradingSettingsService = marginTradingSettingsService;
            _allPairsSubject = realm.Services.GetSubject<InstrumentBidAskPair>(frontSettings.WampPricesTopicName);
        }

        public async Task ProcessPrices(InstrumentBidAskPair bidAskPair)
        {
            _allPairsSubject.OnNext(bidAskPair);
            GetInstrumentPriceSubject(bidAskPair.Instrument).OnNext(bidAskPair);
            await Task.FromResult(0);
        }

        public async Task ProcessAccountChanged(AccountChangedMessage accountChangedMessage)
        {
            if (accountChangedMessage.EventType != AccountEventTypeEnum.Updated)
            {
                _marginTradingSettingsService.ResetCacheForClient(accountChangedMessage.Account?.ClientId);
                return;
            }

            var account = accountChangedMessage.Account;
            string queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get account change from {queueName} queue for clientId = {account.ClientId}");
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
            string queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get order change from {queueName} queue for clientId = {order.ClientId}");

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
            string queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get account stopout from {queueName} queue for clientId = {stopout.ClientId}");

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
            string queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get user update from {queueName} queue for {userUpdate.ClientIds.Length} clients");

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

        private ISubject<InstrumentBidAskPair> GetInstrumentPriceSubject(string instrument)
        {
            return _priceSubjects.GetOrAdd(instrument,
                i => _realm.Services.GetSubject<InstrumentBidAskPair>(
                    $"{_frontSettings.WampPricesTopicName}.{instrument}"));
        }
    }
}
