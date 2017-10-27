using MarginTrading.Common.ClientContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 4014

namespace MarginTrading.Client.Bot
{
    class BotTest
    {
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler<EventArgs> TestFinished;

        readonly List<OperationResult> _operations;
        private string[] _processedScript;

        int _currentAction;
        readonly Timer _actionTimer;

        private InitDataLiveDemoClientResponse _initData;
        private InitChartDataClientResponse _initGraph;

        public BotClient Bot { get; }
        public string[] Actions { get; }
        public bool IsFinished{ get; private set; }

        public List<OperationResult> Operations => _operations;

        public BotTest(BotClient bot, string[] actions)
        {
            IsFinished = false;
            Bot = bot;
            Actions = actions;
            _actionTimer = new Timer(NextActionTimerCall, null, -1, -1);
            _operations = new List<OperationResult>();
        }

        public void RunScriptAsync()
        {
            var t = new Thread(RunScript);
            t.Start();
        }
        private void RunScript()
        {
            _currentAction = 0;
            if (Actions == null || Actions.Length < 1)
                throw new ArgumentException("Actions");

            // Pre-process script
            var pscript = new List<string>();
            for (var i = 0; i < Actions.Length; i++)
            {
                if (Actions[i].ToLower().StartsWith("repeat"))
                {
                    //do repeat stuff
                    if (Actions[i].Split(' ').Length <= 1)
                        continue;

                    var rcount = Actions[i].Split(' ')[1];
                    if (rcount.ToLower() == "all")
                    {                                                        
                        var repeated = new List<string>();
                        for (var j = 0; j < i; j++)
                        {
                            repeated.Add(Actions[j]);
                        }

                        int repeatAllCount = 1;
                        if (Actions[i].Split(' ').Length > 2)
                            int.TryParse(Actions[i].Split(' ')[2], out repeatAllCount);                                    
                            
                        for (var k = 0; k < repeatAllCount; k++)
                        {
                            pscript.AddRange(repeated);
                        }
                    }
                    else
                    {
                        int.TryParse(rcount, out var repeatCount);
                        var pos = -repeatCount;
                        var repeatActions = new List<string>();
                        while (pos < 0)
                        {
                            repeatActions.Add(Actions[i + pos]);
                            pos++;
                        }
                        pscript.AddRange(repeatActions);
                    }
                }
                else
                    pscript.Add(Actions[i]);
            }
            _processedScript = pscript.ToArray();

            Execute(_processedScript[_currentAction]);
        }

        private async Task Execute(string action, bool restartTimer = true)
        {
            // do action
            LogInfo($"Action: {action}");

            var command = action.Split(' ')[0].ToLower();

            switch (command)
            {
                case "initdata":
                    var resinitdata = await Bot.InitData();
                    _initData = (InitDataLiveDemoClientResponse)resinitdata.Result;
                    _operations.Add(resinitdata);
                    break;
                case "initaccounts":
                    var resinitaccounts = await Bot.InitAccounts();                    
                    _operations.Add(resinitaccounts);
                    break;
                case "initgraph":
                    var resinitGraph = await Bot.InitGraph();
                    _initGraph = (InitChartDataClientResponse)resinitGraph.Result;
                    _operations.Add(resinitGraph);
                    foreach (var graphRow in _initGraph.ChartData)
                    {
                        LogInfo($"ChartRow: {graphRow.Key}:{graphRow.Value}");
                    }
                    
                    break;
                case "subscribe":
                    var subscribeInstrument = action.Split(' ')[1].ToUpper();
                    Bot.SubscribePrice(subscribeInstrument);
                    break;
                case "unsubscribe":
                    var unsubscribeInstrument = action.Split(' ')[1].ToUpper();
                    Bot.UnsubscribePrice(unsubscribeInstrument);
                    break;
                case "placeorder":
                    #region placeorder
                    if (_initData == null)
                    {
                        LogInfo("PlaceOrder Failed. InitData not performed, please call InitData before placing orders");
                    }
                    else
                    {
                        var placeOrderInstrument = action.Split(' ')[1].ToUpper();
                        int placeOrderCount;
                        if (action.Split(' ').Length > 2)
                        {
                            string orderCount = action.Split(' ')[2].ToUpper();
                            if (!int.TryParse(orderCount, out placeOrderCount))
                                placeOrderCount = 1;
                        }
                        else
                            placeOrderCount = 1;

                        var result = await Bot.PlaceOrders(_initData.Demo.Accounts[0].Id, placeOrderInstrument, placeOrderCount);
                        _operations.AddRange(result);                        
                    }
                    #endregion
                    break;
                case "closeorder":
                    #region closeorder
                    if (_initData == null)
                    {
                        LogInfo("CloseOrder Failed. InitData not performed, please call InitData before closing orders");
                    }
                    else
                    {
                        string closeOrderInstrument = action.Split(' ')[1].ToUpper();
                        int closeOrderCount;
                        if (action.Split(' ').Length > 2)
                        {
                            string orderCount = action.Split(' ')[2].ToUpper();
                            if (!int.TryParse(orderCount, out closeOrderCount))
                                closeOrderCount = 1;
                        }
                        else
                            closeOrderCount = 1;

                        var result = await Bot.CloseOrders(_initData.Demo.Accounts[0].Id, closeOrderInstrument, closeOrderCount);
                        _operations.AddRange(result);
                    }
                    #endregion
                    break;
                case "cancelorder":
                    #region cancelorder
                    if (_initData == null)
                    {
                        LogInfo("CancelOrder Failed. InitData not performed, please call InitData before canceling orders");
                    }
                    else
                    {
                        string cancelOrderInstrument = action.Split(' ')[1].ToUpper();
                        int cancelOrderCount;
                        if (action.Split(' ').Length > 2)
                        {
                            string orderCount = action.Split(' ')[2].ToUpper();
                            if (!int.TryParse(orderCount, out cancelOrderCount))
                                cancelOrderCount = 1;
                        }
                        else
                            cancelOrderCount = 1;

                        var result = await Bot.CancelOrders(_initData.Demo.Accounts[0].Id, cancelOrderInstrument, cancelOrderCount);
                        _operations.AddRange(result);
                    }
                    #endregion
                    break;
                case "gethistory":
                    var resgethistory = await Bot.GetHistory();
                    _operations.Add(resgethistory);
                    break;
                case "getaccounthistory":
                    var resgetaccounthistory = await Bot.GetAccountHistory();
                    _operations.Add(resgetaccounthistory);
                    break;
                case "getaccountopenpositions":
                    if (_initData == null)
                    {
                        LogInfo("GetAccountOpenPositions Failed. InitData not performed, please call InitData before placing orders");
                    }
                    else
                    {
                        var result = await Bot.GetAccountOpenPositions(_initData.Demo.Accounts[0].Id);
                        _operations.Add(result);
                    }
                    break;
                case "getclientorders":
                    var getClientOrdersResult = await Bot.GetClientOrders();
                    _operations.Add(getClientOrdersResult);
                    break;              
                case "reconnect":
                    Bot.Reconnect();
                    LogInfo("Reconnected...");
                    break;
                case "placependingorder":
                    #region placependingorder
                    if (_initData == null)
                    {
                        LogInfo("PlaceOrder Failed. InitData not performed, please call InitData before placing orders");
                    }
                    else
                    {
                        string placeOrderInstrument = action.Split(' ')[1].ToUpper();
                        int placeOrderCount;
                        if (action.Split(' ').Length > 2)
                        {
                            string orderCount = action.Split(' ')[2].ToUpper();
                            if (!int.TryParse(orderCount, out placeOrderCount))
                                placeOrderCount = 1;
                        }
                        else
                            placeOrderCount = 1;
                        var currentBid = _initData.Prices[placeOrderInstrument].Bid;
                        var result = await Bot.PlacePendingOrders(_initData.Demo.Accounts[0].Id, placeOrderInstrument, placeOrderCount, currentBid);
                        _operations.AddRange(result);
                    }
                    #endregion
                    break;
            }


            // Wait for next action
            if (restartTimer)
                _actionTimer.Change(Bot.ActionScriptInterval, 0);
        }
        private void NextActionTimerCall(object status)
        {
            _actionTimer.Change(-1, -1);
            _currentAction++;
            if (_currentAction >= _processedScript.Length)
            {
                IsFinished = true;
                PrintTestOperations();
                //script finished
                OnTestFinished(new EventArgs());            }
            else
            {
                Execute(_processedScript[_currentAction]);                
            }
        }

        private void PrintTestOperations()
        {
            var distinct = _operations.GroupBy(x => x.Operation);
            Console.WriteLine(distinct);
            LogInfo($" == Test Finished for Bot {Bot.Id} ==");
            foreach (var group in distinct)
            {
                LogInfo($"{group.Key}=>Count:{group.Count()} Average Time:{group.Average(x => x.Duration.TotalSeconds)}");
            }
            LogInfo(" ==  ==");
        }

        private void LogInfo(string message)
        {
            OnLog(new LogEventArgs(DateTime.UtcNow, $"Bot:[{Bot.Id}]", "info", $"Thread[{ Thread.CurrentThread.ManagedThreadId.ToString() }] {message}", null));
        }

        private void OnLog(LogEventArgs e)
        {
            LogEvent?.Invoke(this, e);
        }
        private void OnTestFinished(EventArgs e)
        {
            TestFinished?.Invoke(this, e);
        }
    }
}
