using Common;
using Flurl;
using Flurl.Http;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Wamp;
using MarginTrading.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.V2;

namespace MarginTrading.Client
{
    class BotClient : MtClient, IDisposable
    {
        public event EventHandler<LogEventArgs> LogEvent;

        TestBotUserSettings _settings;
        Timer _transactionTimer = null;
        Timer _connectTimer = null;
        bool _isDisposing = false;
        string _authorizationAddress;
        IDisposable notificationSubscription;
        Random random;


        Dictionary<string, IDisposable> PriceSubscription;
        Dictionary<string, int> SubscriptionHistory;

        public int Id { get { return _settings.Number; } }
        public string Email { get { return _settings.Email; } }
        public string Password { get { return _settings.Password; } }

        public int ActionScriptInterval { get; private set; }
        public int TransactionFrequencyMin { get; private set; }
        public int TransactionFrequencyMax { get; private set; }
        public bool Initialized { get; private set; }

        public BotClient(TestBotUserSettings settings)
        {
            _settings = settings;
            Initialized = false;
            PriceSubscription = new Dictionary<string, IDisposable>();
            SubscriptionHistory = new Dictionary<string, int>();
            random = new Random();
        }

        private void Connect()
        {
            var factory = new DefaultWampChannelFactory();
            _channel = factory.CreateJsonChannel(_serverAddress, "mtcrossbar");
            int tries = 0;
            while (!_channel.RealmProxy.Monitor.IsConnected)
            {
                try
                {
                    tries++;
                    LogInfo($"Trying to connect to server {_serverAddress }...");
                    _channel.Open().Wait();
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    if (tries > 5)
                        throw;
                    LogInfo("Retrying in 5 sec...");

                }
            }
            LogInfo($"Connected to server {_serverAddress}");

            _realmProxy = _channel.RealmProxy;
            _service = _realmProxy.Services.GetCalleeProxy<IRpcMtFrontend>();

            // Subscribe Notifications
            notificationSubscription = _realmProxy.Services.GetSubject<NotifyResponse>($"user.updates.{_notificationId}").Subscribe(NotificationReceived);
        }
        private void Disconnect()
        {
            LogInfo($"Disconnecting from server {_serverAddress}");
            Close();
        }
        public async Task Initialize(string serverAddress, string authorizationAddress, int actionScriptInterval, int transactionFrequencyMin, int transactionFrequencyMax)
        {
            
            LogInfo($"Initializing bot {_settings.Number}. AquireTokenData...");
            _serverAddress = serverAddress;
            _authorizationAddress = authorizationAddress;
            ActionScriptInterval = actionScriptInterval;
            TransactionFrequencyMin = transactionFrequencyMin;
            TransactionFrequencyMax = transactionFrequencyMax;
            
            try
            {
                var res = await AquireTokenData();
                _token = res.token;
                _notificationId = res.notificationsId;
                Initialized = true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
            
            Connect();        
        }
        public void Reconnect()
        {
            Disconnect();
            System.Threading.Thread.Sleep(2000);
            Connect();
        }

        public new void IsAlive()
        {
            try
            {
                var data = _service.IsAlive();
                LogInfo($"IsAlive:{data.ToJson()}");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
        public new async Task<InitDataLiveDemoClientResponse> InitData()
        {
            try
            {
                var _initData = await _service.InitData(_token);
                LogInfo($"InitData: Assets={_initData.Assets.Length} Prices={_initData.Prices.Count}");
                return _initData;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }
        public new async Task<InitAccountsLiveDemoClientResponse> InitAccounts()
        {
            try
            {
                var _initAccounts = await _service.InitAccounts(_token);
                LogInfo($"InitAccounts: Demo={_initAccounts.Demo.Length} Live={_initAccounts.Live.Length}");
                return _initAccounts;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }
                
        public new async Task<InitChartDataClientResponse> InitGraph()
        {
            try
            {
                var _chartData = await _service.InitGraph();
                LogInfo($"InitGraph: ChartData={_chartData.ChartData.Count}");
                return _chartData;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }

        }
        public new async Task<AccountHistoryClientResponse> GetAccountHistory()
        {
            try
            {
                var request = new AccountHistoryRpcClientRequest
                {
                    Token = _token
                };

                var result = await _service.GetAccountHistory(request.ToJson());
                LogInfo($"GetAccountHistory: Accounts={result.Account.Length}, OpenPositions={result.OpenPositions.Length}, PositionsHistory={result.PositionsHistory.Length}");
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }

        public new async Task<AccountHistoryItemClient[]> GetHistory()
        {
            var request = new AccountHistoryRpcClientRequest
            {
                Token = _token
            };

            var result = await _service.GetHistory(request.ToJson());
            LogInfo($"GetHistory: Items={result.Length}");
            return result;
        }
        public async Task<IEnumerable<OrderClientContract>> GetOpenPositionsFromDemo()
        {
            var result  = await _service.GetOpenPositions(_token);
            return result.Demo;
        }
        public async Task<IEnumerable<OrderClientContract>> GetAccountOpenPositions(string accountId)
        {
            var request = new AccountTokenClientRequest
            {
                Token = _token,
                AccountId = accountId
            };

            var result = await _service.GetAccountOpenPositions(request.ToJson());
            LogInfo($"GetAccountOpenPositions: OpenPositions={result.Length}");
            return result;
        }
        public new async Task<ClientPositionsLiveDemoClientResponse> GetClientOrders()
        {
            var result = await _service.GetClientOrders(_token);
            LogInfo($"GetClientOrders: Orders={result.Demo.Orders.Length}, Positions={result.Demo.Positions.Length}");
            return result;
        }

        public async Task<IEnumerable<OrderClientContract>> PlaceOrders(string accountId, string instrument, int numOrders)
        {
            List<OrderClientContract> result = new List<OrderClientContract>();
            for (int i = 0; i < numOrders; i++)
            {
                try
                {
                    var request = new OpenOrderRpcClientRequest
                    {
                        Token = _token,
                        Order = new NewOrderClientContract
                        {
                            AccountId = accountId,
                            FillType = OrderFillType.FillOrKill,
                            Instrument = instrument,
                            Volume = 1
                        }
                    };

                    LogInfo($"Placing order {i+1}/{numOrders}: [{instrument}]");
                    var order = await _service.PlaceOrder(request.ToJson());
                    result.Add(order.Result);

                    if (order.Result.Status == 3)
                        LogInfo($"Order rejected: {order.Result.RejectReason} -> {order.Result.RejectReasonText}");
                    else
                        LogInfo($"Order placed: {order.Result.Id} -> Status={order.Result.Status}");                    
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                // Sleep TransactionFrequency
                Thread.Sleep(GetRandomTransactionInterval());
            }
            return result;
        }
        public async Task<IEnumerable<OrderClientContract>> PlacePendingOrders(string accountId, string instrument, int numOrders, double currentBid)
        {
            List<OrderClientContract> result = new List<OrderClientContract>();

            for (int i = 0; i < numOrders; i++)
            {
                try
                {
                    var request = new OpenOrderRpcClientRequest
                    {
                        Token = _token,
                        Order = new NewOrderClientContract
                        {
                            AccountId = accountId,
                            FillType = OrderFillType.FillOrKill,
                            Instrument = instrument,
                            Volume = 1,
                            ExpectedOpenPrice = currentBid * 0.9
                        }
                    };

                    LogInfo($"Placing order {i + 1}/{numOrders}: [{instrument}]");
                    var order = await _service.PlaceOrder(request.ToJson());
                    result.Add(order.Result);

                    if (order.Result.Status == 3)
                        LogInfo($"Order rejected: {order.Result.RejectReason} -> {order.Result.RejectReasonText}");
                    else
                        LogInfo($"Order placed: {order.Result.Id} -> Status={order.Result.Status}");
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                // Sleep TransactionFrequency
                Thread.Sleep(GetRandomTransactionInterval());
            }
            return result;
        }
        public async Task<IEnumerable<bool>> CloseOrders(string accountId, string instrument, int numOrders)
        {
            List<bool> result = new List<bool>();
            List<OrderClientContract> orders = (await GetOpenPositionsFromDemo())
                .Where(x => x.AccountId == accountId && x.Instrument == instrument).ToList();
            int processed = 0;
            foreach (var order in orders)
            {
                if (processed >= numOrders)
                    break;
                try
                {
                    LogInfo($"Closing order {processed + 1}/{numOrders}: [{instrument}] Id={order.Id} Fpl={order.Fpl}");
                    var request = new CloseOrderRpcClientRequest
                    {
                        OrderId = order.Id,
                        AccountId = order.AccountId,
                        Token = _token
                    };

                    var orderClosed = await _service.CloseOrder(request.ToJson());
                    result.Add(orderClosed.Result);
                    if (orderClosed.Result)
                        LogInfo($"Order Closed Id={order.Id}");
                    else
                        LogInfo($"Order Close Failed Id={order.Id} Message:{orderClosed.Message}");                    
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                processed++;
                // Sleep TransactionFrequency
                Thread.Sleep(GetRandomTransactionInterval());
            }

            if (processed < numOrders)
                LogWarning($"Not enough orders to close requested amount (numOrders)");

            return result;
        }
        public async Task<IEnumerable<bool>> CancelOrders(string accountId, string instrument, int numOrders)
        {
            List<bool> result = new List<bool>();
            List<OrderClientContract> orders = (await GetOpenPositionsFromDemo())
                .Where(x => x.AccountId == accountId && x.Instrument == instrument).ToList();
            int processed = 0;
            foreach (var order in orders)
            {
                if (processed >= numOrders)
                    break;
                try
                {
                    LogInfo($"Canceling order {processed + 1}/{numOrders}: [{instrument}] Id={order.Id} Fpl={order.Fpl}");
                    var request = new CloseOrderRpcClientRequest
                    {
                        OrderId = order.Id,
                        Token = _token
                    };

                    var ordercanceled = await _service.CancelOrder(request.ToJson());                    
                    result.Add(ordercanceled.Result);
                    if (ordercanceled.Result)
                        LogInfo($"Order Canceled Id={order.Id}");
                    else
                        LogInfo($"Order Cancel Failed Id={order.Id} Message:{ordercanceled.Message}");                    
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                processed++;
                // Sleep TransactionFrequency
                Thread.Sleep(GetRandomTransactionInterval());
            }

            if (processed < numOrders)
                LogWarning($"Not enough orders to close requested amount (numOrders)");

            return result;
        }

        public void SubscribePrice(string instrument)
        {
            var topicName = !string.IsNullOrEmpty(instrument) ? $"prices.update.{instrument}" : "prices.update";
            IDisposable subscription = _realmProxy.Services.GetSubject<InstrumentBidAskPair>(topicName)
                .Subscribe(PriceReceived);

            PriceSubscription.Add(instrument, subscription);
            LogInfo($"SubscribePrice: Instrument={instrument}");

            //subscription.Dispose();
        }
        public void UnsubscribePrice(string instrument)
        {
            IDisposable subscription = PriceSubscription[instrument];
            if (subscription != null)
                subscription.Dispose();

            if (SubscriptionHistory.ContainsKey(instrument))
            {
                int received = SubscriptionHistory[instrument];
                LogInfo($"UnsubscribePrice: Instrument={instrument}. Entries received:{received}");
                SubscriptionHistory.Remove(instrument);
            }
        }

        private void PriceReceived(InstrumentBidAskPair price)
        {
            if (!SubscriptionHistory.ContainsKey(price.Instrument))
                SubscriptionHistory.Add(price.Instrument, 0);

            int received = SubscriptionHistory[price.Instrument];
            SubscriptionHistory[price.Instrument] = received + 1;

            //LogInfo($"Price received:{price.Instrument} Ask/Bid:{price.Ask}/{price.Bid}");
        }

        private void NotificationReceived(NotifyResponse info)
        {
            if (info.Account != null)
                LogInfo($"Notification received: Account changed={info.Account.Id} Balance:{info.Account.Balance}");

            if (info.Order != null)
                LogInfo($"Notification received: Order changed={info.Order.Id} Open:{info.Order.OpenDate} Close:{info.Order.CloseDate} Fpl:{info.Order.Fpl}");

            if (info.AccountStopout != null)
                LogInfo($"Notification received: Account stopout={info.AccountStopout.AccountId}");            

            if (info.UserUpdate != null)
                LogInfo($"Notification received: User update={info.UserUpdate.UpdateAccountAssetPairs}, accounts = {info.UserUpdate.UpdateAccounts}");
            
        }

        private async Task<(string token, string notificationsId)> AquireTokenData()
        {
            var result = await _authorizationAddress.PostJsonAsync(new
            {
                _settings.Email,
                _settings.Password                
            })
            .ReceiveJson<ApiAuthResult>();
            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return (token: result.Result.Token, notificationsId: result.Result.NotificationsId);
        }

        private int GetRandomTransactionInterval()
        {
            return random.Next(TransactionFrequencyMin, TransactionFrequencyMax);
        }

        private void LogInfo(string message)
        {
            OnLog(new LogEventArgs(DateTime.UtcNow, $"Bot:[{_settings.Number}]", "info", $"Thread[{ Thread.CurrentThread.ManagedThreadId.ToString() }] {message}", null));
        }
        private void LogWarning(string message)
        {
            OnLog(new LogEventArgs(DateTime.UtcNow, $"Bot:[{_settings.Number}]", "warning", $"Thread[{ Thread.CurrentThread.ManagedThreadId.ToString() }] {message}", null));
        }
        private void LogError(Exception error)
        {
            OnLog(new LogEventArgs(DateTime.UtcNow, $"Bot:[{_settings.Number}]", "error", $"Thread[{ Thread.CurrentThread.ManagedThreadId.ToString() }] {error.Message}", error));
        }
        private void OnLog(LogEventArgs e)
        {
            LogEvent?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_isDisposing)
                return;
            _isDisposing = true;

            if (notificationSubscription != null)
                notificationSubscription.Dispose();

            if (_channel != null)
                Close();
            if (_transactionTimer != null)
            {
                _transactionTimer.Dispose();
                _transactionTimer = null;
            }

            if (_connectTimer != null)
            {
                _connectTimer.Dispose();
                _connectTimer = null;
            }
        }
    }

    class ApiAuthResult
    {
        [JsonProperty("Result")]
        public AuthResult Result { get; set; }
        [JsonProperty("Error")]
        public AuthError Error { get; set; }
    }
    class AuthResult
    {

        [JsonProperty("KycStatus")]
        public string KycStatus { get; set; }
        [JsonProperty("PinIsEntered ")]
        public bool PinIsEntered { get; set; }
        [JsonProperty("Token")]
        public string Token { get; set; }
        [JsonProperty("NotificationsId")]
        public string NotificationsId { get; set; }
    }
    class AuthError
    {
        [JsonProperty("Code")]
        public int Code { get; set; }
        [JsonProperty("Field ")]
        public object Field { get; set; }
        [JsonProperty("Message")]
        public string Message { get; set; }
    }
}
