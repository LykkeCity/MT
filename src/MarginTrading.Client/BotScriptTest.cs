﻿using MarginTrading.Common.ClientContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarginTrading.Client
{
    class BotScriptTest
    {
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler<EventArgs> TestFinished;

        List<OperationResult> operations;

        int currentAction;
        Timer actionTimer;

        InitDataLiveDemoClientResponse initData = null;
        InitChartDataClientResponse initGraph = null;
        

        public BotClient Bot { get; private set; }
        public string[] Actions { get; private set; }
        private string[] processedScript;
        public bool IsFinished{ get; private set; }

        public List<OperationResult> Operations { get { return operations; } }

        public BotScriptTest(BotClient bot, string[] actions)
        {
            IsFinished = false;
            Bot = bot;
            Actions = actions;
            actionTimer = new Timer(NextActionTimerCall, null, -1, -1);
            operations = new List<OperationResult>();
        }

        public void RunScriptAsync()
        {
            Thread t = new Thread(new System.Threading.ThreadStart(RunScript));
            t.Start();

        }
        private void RunScript()
        {
            currentAction = 0;
            if (Actions == null || Actions.Length < 1)
                throw new ArgumentException("Actions");

            // Pre-process script
            List<string> pscript = new List<string>();
            for (int i = 0; i < Actions.Length; i++)
            {
                if (Actions[i].ToLower().StartsWith("repeat"))
                {
                    int repeatCount = 1;
                    //do repeat stuff
                    if (Actions[i].Split(' ').Length > 1)
                    {
                        string rcount = Actions[i].Split(' ')[1];
                        if (rcount.ToLower() == "all")
                        {                                                        
                            List<string> repeated = new List<string>();
                            for (int j = 0; j < i; j++)
                            {
                                repeated.Add(Actions[j]);
                            }

                            int repeatAllCount = 1;
                            if (Actions[i].Split(' ').Length > 2)
                                int.TryParse(Actions[i].Split(' ')[2], out repeatAllCount);                                    
                            
                            for (int k = 0; k < repeatAllCount; k++)
                            {
                                pscript.AddRange(repeated);
                            }
                        }
                        else
                        {
                            int.TryParse(rcount, out repeatCount);
                            int pos = -repeatCount;
                            List<string> repeatActions = new List<string>();
                            while (pos < 0)
                            {
                                repeatActions.Add(Actions[i + pos]);
                                pos++;
                            }
                            pscript.AddRange(repeatActions);
                        }
                    }
                    
                }
                else
                    pscript.Add(Actions[i]);
            }
            processedScript = pscript.ToArray();

            Execute(processedScript[currentAction]);
        }

        private async Task Execute(string action, bool restartTimer = true)
        {
            // do action
            LogInfo($"Action: {action}");

            string command = action.Split(' ')[0].ToLower();

            switch (command)
            {
                case "initdata":
                    var resinitdata = await Bot.InitData();
                    initData = (InitDataLiveDemoClientResponse)resinitdata.Result;
                    operations.Add(resinitdata);
                    break;
                case "initaccounts":
                    var resinitaccounts = await Bot.InitAccounts();                    
                    operations.Add(resinitaccounts);
                    break;
                case "initgraph":
                    var resinitGraph = await Bot.InitGraph();
                    initGraph = (InitChartDataClientResponse)resinitGraph.Result;
                    operations.Add(resinitGraph);
                    break;
                case "subscribe":
                    string subscribeInstrument = action.Split(' ')[1].ToUpper();
                    Bot.SubscribePrice(subscribeInstrument);
                    break;
                case "unsubscribe":
                    string unsubscribeInstrument = action.Split(' ')[1].ToUpper();
                    Bot.UnsubscribePrice(unsubscribeInstrument);
                    break;
                case "placeorder":
                    #region placeorder
                    if (initData == null)
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

                        var result = await Bot.PlaceOrders(initData.Demo.Accounts[0].Id, placeOrderInstrument, placeOrderCount);
                        operations.AddRange(result);                        
                    }
                    #endregion
                    break;
                case "closeorder":
                    #region closeorder
                    if (initData == null)
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

                        var result = await Bot.CloseOrders(initData.Demo.Accounts[0].Id, closeOrderInstrument, closeOrderCount);
                        operations.AddRange(result);
                    }
                    #endregion
                    break;
                case "cancelorder":
                    #region cancelorder
                    if (initData == null)
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

                        var result = await Bot.CancelOrders(initData.Demo.Accounts[0].Id, cancelOrderInstrument, cancelOrderCount);
                        operations.AddRange(result);
                    }
                    #endregion
                    break;
                case "gethistory":
                    var resgethistory = await Bot.GetHistory();
                    operations.Add(resgethistory);
                    break;
                case "getaccounthistory":
                    var resgetaccounthistory = await Bot.GetAccountHistory();
                    operations.Add(resgetaccounthistory);
                    break;
                case "getaccountopenpositions":
                    if (initData == null)
                    {
                        LogInfo("GetAccountOpenPositions Failed. InitData not performed, please call InitData before placing orders");
                    }
                    else
                    {
                        var result = await Bot.GetAccountOpenPositions(initData.Demo.Accounts[0].Id);
                        operations.Add(result);
                    }
                    break;
                case "getclientorders":
                    var GetClientOrdersResult = await Bot.GetClientOrders();
                    operations.Add(GetClientOrdersResult);
                    break;              
                case "reconnect":
                    Bot.Reconnect();
                    LogInfo("Reconnected...");
                    break;
                case "placependingorder":
                    #region placependingorder
                    if (initData == null)
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
                        var currentBid = initData.Prices[placeOrderInstrument].Bid;
                        var result = await Bot.PlacePendingOrders(initData.Demo.Accounts[0].Id, placeOrderInstrument, placeOrderCount, currentBid);
                        operations.AddRange(result);
                    }
                    #endregion
                    break;
                default:
                    break;
            }


            // Wait for next action
            if (restartTimer)
                actionTimer.Change(Bot.ActionScriptInterval, 0);
        }
        private void NextActionTimerCall(object status)
        {
            actionTimer.Change(-1, -1);
            currentAction++;
            if (currentAction >= processedScript.Length)
            {
                IsFinished = true;
                PrintTestOperations();
                //script finished
                OnTestFinished(new EventArgs());            }
            else
            {
                Execute(processedScript[currentAction]);                
            }
        }

        private void PrintTestOperations()
        {
            var distinct = operations.GroupBy(x => x.Operation);
            Console.WriteLine(distinct);
            LogInfo($" == Test Finished for Bot {Bot.Id} ==");
            foreach (var group in distinct)
            {
                LogInfo($"{group.Key}=>Count:{group.Count()} Average Time:{group.Average(x => x.Duration.TotalSeconds)}");
            }
            LogInfo($" ==  ==");
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