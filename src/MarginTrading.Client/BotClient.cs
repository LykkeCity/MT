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
        public new async Task<OperationResult> InitData()
        {
            try
            {
                OperationResult res = new OperationResult
                {
                    Operation = "InitData",
                    StartDate = DateTime.UtcNow
                };                
                var _initData = await _service.InitData(_token);
                res.EndDate = DateTime.UtcNow;
                res.Result = _initData;
                LogInfo($";{res.Duration};InitData: Assets={_initData.Assets.Length} Prices={_initData.Prices.Count}");
                
                return res;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }
        public new async Task<OperationResult> InitAccounts()
        {
            try
            {
                OperationResult res = new OperationResult
                {
                    Operation = "InitAccounts",
                    StartDate = DateTime.UtcNow
                };
                var _initAccounts = await _service.InitAccounts(_token);
                res.EndDate = DateTime.UtcNow;
                res.Result = _initAccounts;
                LogInfo($";{res.Duration};InitAccounts: Demo={_initAccounts.Demo.Length} Live={_initAccounts.Live.Length}");
                return res;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }
                
        public new async Task<OperationResult> InitGraph()
        {
            try
            {
                OperationResult res = new OperationResult
                {
                    Operation = "InitGraph",
                    StartDate = DateTime.UtcNow
                };
                InitChartDataClientResponse _chartData = await _service.InitGraph();
                res.EndDate = DateTime.UtcNow;
                res.Result = _chartData;
                LogInfo($";{res.Duration};InitGraph: ChartData={_chartData.ChartData.Count}");
                return res;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }

        }
        public new async Task<OperationResult> GetAccountHistory()
        {
            try
            {
                OperationResult res = new OperationResult
                {
                    Operation = "GetAccountHistory",
                    StartDate = DateTime.UtcNow
                };

                var request = new AccountHistoryRpcClientRequest
                {
                    Token = _token
                };
                AccountHistoryClientResponse result = await _service.GetAccountHistory(request.ToJson());
                res.EndDate = DateTime.UtcNow;
                res.Result = result;
                LogInfo($";{res.Duration};GetAccountHistory: Accounts={result.Account.Length}, OpenPositions={result.OpenPositions.Length}, PositionsHistory={result.PositionsHistory.Length}");
                return res;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return null;
            }
        }

        public new async Task<OperationResult> GetHistory()
        {
            OperationResult res = new OperationResult
            {
                Operation = "GetHistory",
                StartDate = DateTime.UtcNow
            };

            var request = new AccountHistoryRpcClientRequest
            {
                Token = _token
            };
            AccountHistoryItemClient[] result = await _service.GetHistory(request.ToJson());
            res.EndDate = DateTime.UtcNow;
            res.Result = result;
            LogInfo($";{res.Duration};GetHistory: Items={result.Length}");
            return res;
        }
        public async Task<OperationResult> GetOpenPositionsFromDemo()
        {
            OperationResult res = new OperationResult
            {
                Operation = "GetOpenPositions",
                StartDate = DateTime.UtcNow
            };

            var result  = await _service.GetOpenPositions(_token);
            res.EndDate = DateTime.UtcNow;
            res.Result = result.Demo;
            LogInfo($";{res.Duration};GetOpenPositionsFromDemo: Items={result.Demo.Length}");
            return res;
        }
        public async Task<OperationResult> GetAccountOpenPositions(string accountId)
        {
            OperationResult res = new OperationResult
            {
                Operation = "GetAccountOpenPositions",
                StartDate = DateTime.UtcNow
            };
            var request = new AccountTokenClientRequest
            {
                Token = _token,
                AccountId = accountId
            };
            var result = await _service.GetAccountOpenPositions(request.ToJson());
            res.EndDate = DateTime.UtcNow;
            res.Result = result;
            LogInfo($";{res.Duration};GetAccountOpenPositions: OpenPositions={result.Length}");
            return res;
        }
        public new async Task<OperationResult> GetClientOrders()
        {
            OperationResult res = new OperationResult
            {
                Operation = "GetClientOrders",
                StartDate = DateTime.UtcNow
            };
            
            ClientPositionsLiveDemoClientResponse result = await _service.GetClientOrders(_token);
            res.EndDate = DateTime.UtcNow;
            res.Result = result;
            LogInfo($";{res.Duration};GetClientOrders: Orders={result.Demo.Orders.Length}, Positions={result.Demo.Positions.Length}");
            return res;
        }

        public async Task<IEnumerable<OperationResult>> PlaceOrders(string accountId, string instrument, int numOrders)
        {            
            List<OperationResult> operations = new List<OperationResult>();
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

                    OperationResult res = new OperationResult
                    {
                        Operation = "PlaceOrders",
                        StartDate = DateTime.UtcNow
                    };
                    var order = await _service.PlaceOrder(request.ToJson());
                    res.EndDate = DateTime.UtcNow;
                    res.Result = order.Result;
                    operations.Add(res);

                    if (order.Result.Status == 3)
                        LogInfo($";{res.Duration};Order rejected: {order.Result.RejectReason} -> {order.Result.RejectReasonText}");
                    else
                        LogInfo($";{res.Duration};Order placed: {order.Result.Id} -> Status={order.Result.Status}");
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                // Sleep TransactionFrequency
                Thread.Sleep(GetRandomTransactionInterval());
            }
            return operations;
        }
        public async Task<IEnumerable<OperationResult>> PlacePendingOrders(string accountId, string instrument, int numOrders, double currentBid)
        {
            
            List<OperationResult> operations = new List<OperationResult>();
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
                    OperationResult res = new OperationResult
                    {
                        Operation = "PlacePendingOrders",
                        StartDate = DateTime.UtcNow
                    };
                    var order = await _service.PlaceOrder(request.ToJson());
                    res.EndDate = DateTime.UtcNow;
                    res.Result = order.Result;
                    operations.Add(res);

                    if (order.Result.Status == 3)
                        LogInfo($";{res.Duration};Order rejected: {order.Result.RejectReason} -> {order.Result.RejectReasonText}");
                    else
                        LogInfo($";{res.Duration};Order placed: {order.Result.Id} -> Status={order.Result.Status}");
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                // Sleep TransactionFrequency
                Thread.Sleep(GetRandomTransactionInterval());
            }
            return operations;
        }
        public async Task<IEnumerable<OperationResult>> CloseOrders(string accountId, string instrument, int numOrders)
        {
            List<OperationResult> operations = new List<OperationResult>();
            OperationResult resGetOpenPositions = await GetOpenPositionsFromDemo();
            operations.Add(resGetOpenPositions);

            List<OrderClientContract> orders = ((OrderClientContract[])resGetOpenPositions.Result)
                .Where(x => x.AccountId == accountId && x.Instrument == instrument).ToList();
            int processed = 0;
            foreach (var order in orders)
            {
                if (processed >= numOrders)
                    break;
                try
                {
                    LogInfo($"Closing order {processed + 1}/{numOrders}: [{instrument}] Id={order.Id} Fpl={order.Fpl}");
                    OperationResult res = new OperationResult
                    {
                        Operation = "CloseOrders",
                        StartDate = DateTime.UtcNow
                    };                    
                    var request = new CloseOrderRpcClientRequest
                    {
                        OrderId = order.Id,
                        AccountId = order.AccountId,
                        Token = _token
                    };                    
                    var orderClosed = await _service.CloseOrder(request.ToJson());
                    res.EndDate = DateTime.UtcNow;
                    res.Result = orderClosed;
                    operations.Add(res);
                    
                    if (orderClosed.Result)
                        LogInfo($";{res.Duration};Order Closed Id={order.Id}");
                    else
                        LogInfo($";{res.Duration};Order Close Failed Id={order.Id} Message:{orderClosed.Message}");
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

            return operations;
        }
        public async Task<IEnumerable<OperationResult>> CancelOrders(string accountId, string instrument, int numOrders)
        {
            List<OperationResult> operations = new List<OperationResult>();
            OperationResult resGetOpenPositions = await GetOpenPositionsFromDemo();
            operations.Add(resGetOpenPositions);

            List<OrderClientContract> orders = ((OrderClientContract[])resGetOpenPositions.Result)
                .Where(x => x.AccountId == accountId && x.Instrument == instrument && x.Status==0).ToList();
            int processed = 0;
            foreach (var order in orders)
            {
                if (processed >= numOrders)
                    break;
                try
                {
                    LogInfo($"Canceling order {processed + 1}/{numOrders}: [{instrument}] Id={order.Id} Fpl={order.Fpl}");
                    OperationResult res = new OperationResult
                    {
                        Operation = "CancelOrders",
                        StartDate = DateTime.UtcNow
                    };                    
                    var request = new CloseOrderRpcClientRequest
                    {
                        OrderId = order.Id,
                        AccountId = order.AccountId,
                        Token = _token
                    };
                    var ordercanceled = await _service.CancelOrder(request.ToJson());
                    res.EndDate = DateTime.UtcNow;
                    res.Result = ordercanceled;
                    operations.Add(res);

                    if (ordercanceled.Result)
                        LogInfo($";{res.Duration};Order Canceled Id={order.Id}");
                    else
                        LogInfo($";{res.Duration};Order Cancel Failed Id={order.Id} Message:{ordercanceled.Message}");                    
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

            return operations;
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
            string address = $"{_authorizationAddress}/Auth";
            var result = await address.PostJsonAsync(new
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

    public class OperationResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Operation { get; set; }
        public object Result { get; set; }

        public TimeSpan Duration { get { return EndDate - StartDate; } }
    }
}
