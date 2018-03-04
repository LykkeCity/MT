using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.RabbitMqMessages;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Common.Settings;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Contract.ClientContracts;
using MarginTrading.Contract.Mappers;
using MarginTrading.Contract.RabbitMqMessageModels;
using MarginTrading.Frontend.Settings;
using MarginTrading.Frontend.Wamp;
using WampSharp.V2.Realm;

namespace MarginTrading.Frontend
{
    public class RabbitMqHandler
    {
        private readonly IWampHostedRealm _realm;
        private readonly IClientAccountService _clientNotificationService;
        private readonly IMarginTradingOperationsLogService _operationsLog;
        private readonly MtFrontendSettings _settings;
        private readonly IConsole _consoleWriter;
        private readonly ILog _log;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly ISubject<BidAskPairRabbitMqContract> _allPairsSubject;
        private readonly ISubject<TradeClientContract> _tradesSubject;

        private readonly ConcurrentDictionary<string, ISubject<BidAskPairRabbitMqContract>> _priceSubjects =
            new ConcurrentDictionary<string, ISubject<BidAskPairRabbitMqContract>>();

        public RabbitMqHandler(
            IWampHostedRealm realm,
            IClientAccountService clientNotificationService,
            IMarginTradingOperationsLogService operationsLogService,
            MtFrontendSettings settings,
            IConsole consoleWriter,
            ILog log,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService)
        {
            _realm = realm;
            _clientNotificationService = clientNotificationService;
            _operationsLog = operationsLogService;
            _settings = settings;
            _consoleWriter = consoleWriter;
            _log = log;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
            _allPairsSubject = realm.Services.GetSubject<BidAskPairRabbitMqContract>(WampConstants.PricesTopicPrefix);
            _tradesSubject = realm.Services.GetSubject<TradeClientContract>(WampConstants.TradesTopic);
        }

        public Task ProcessPrices(BidAskPairRabbitMqContract bidAskPair)
        {
            try
            {
                _allPairsSubject.OnNext(bidAskPair);
                GetInstrumentPriceSubject(bidAskPair.Instrument).OnNext(bidAskPair);
            }
            catch (Exception e)
            {
                _log.WriteWarning("RabbitMqHandler", "ProcessPrices", bidAskPair.ToJson(), e);
            }
           
            return Task.CompletedTask;
        }
        
        public async Task ProcessTrades(TradeContract trade)
        {
            var contract = new TradeClientContract
            {
                Id = trade.Id,
                AssetPairId = trade.AssetPairId,
                Date = trade.Date,
                OrderId = trade.OrderId,
                Price = trade.Price,
                Type = trade.Type.ToType<TradeClientType>(),
                Volume = trade.Volume
            };
            
            _tradesSubject.OnNext(contract);
            await Task.FromResult(0);
        }

        public async Task ProcessAccountChanged(AccountChangedMessage accountChangedMessage)
        {
            if (accountChangedMessage.EventType != AccountEventTypeEnum.Updated)
                return;

            var account = accountChangedMessage.Account;
            var queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.AccountChanged.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get account change from {queueName} queue for clientId = {account.ClientId}");
            var notificationId = await _clientNotificationService.GetNotificationId(account.ClientId);
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
            var queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.OrderChanged.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get order change from {queueName} queue for clientId = {order.ClientId}");

            var notificationId = await _clientNotificationService.GetNotificationId(order.ClientId);
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
            var queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.AccountStopout.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get account stopout from {queueName} queue for clientId = {stopout.ClientId}");

            var notificationId = await _clientNotificationService.GetNotificationId(stopout.ClientId);
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
            var queueName = QueueHelper.BuildQueueName(_settings.MarginTradingFront.RabbitMqQueues.UserUpdates.ExchangeName, _settings.MarginTradingFront.Env);
            _consoleWriter.WriteLine($"Get user update from {queueName} queue for {userUpdate.ClientIds.Length} clients");

            foreach (var clientId in userUpdate.ClientIds)
            {
                try
                {
                    var notificationId = await _clientNotificationService.GetNotificationId(clientId);
                    var userTopic =
                        _realm.Services.GetSubject<NotifyResponse<UserUpdateEntityClientContract>>(
                            $"user.{notificationId}");

                    var response = new NotifyResponse<UserUpdateEntityClientContract>
                    {
                        Entity = userUpdate.ToClientContract(),
                        Type = NotifyEntityType.UserUpdate
                    };

                    userTopic.OnNext(response);

                    var eventType = string.Empty;

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

        private ISubject<BidAskPairRabbitMqContract> GetInstrumentPriceSubject(string instrument)
        {
            return _priceSubjects.GetOrAdd(instrument,
                i => _realm.Services.GetSubject<BidAskPairRabbitMqContract>(
                    $"{WampConstants.PricesTopicPrefix}.{instrument}"));
        }

        public Task ProcessMarginTradingEnabledChanged(MarginTradingEnabledChangedMessage message)
        {
            _marginTradingSettingsCacheService.OnMarginTradingEnabledChanged(message);
            return Task.CompletedTask;
        }
    }
}
