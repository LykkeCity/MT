using MarginTrading.Common.ClientContracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MarginTrading.Client
{
    class BotScriptTest
    {
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler<EventArgs> TestFinished;

        int currentAction;
        Timer actionTimer;

        InitDataLiveDemoClientResponse initData = null;
        InitChartDataClientResponse initGraph = null;

        public BotClient Bot { get; private set; }
        public string[] Actions { get; private set; }

        public BotScriptTest(BotClient bot, string[] actions)
        {
            Bot = bot;
            Actions = actions;
            actionTimer = new Timer(NextActionTimerCall, null, -1, -1);
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

            Execute(Actions[currentAction]);
        }

        private async void Execute(string action)
        {
            // do action
            LogInfo($"Executing Action: {action}");

            string command = action.Split(' ')[0].ToLower();

            switch (command)
            {
                case "initdata":
                    initData = await Bot.InitData();
                    break;
                case "initgraph":
                    initGraph = await Bot.InitGraph();
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
                        foreach (var item in result)
                        {
                            LogInfo($"PlaceOrder result: Order={item.Id} Instrument={item.Instrument} Status={item.Status}");
                        }
                    }
                    break;
                case "closeorder":
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
                        foreach (var item in result)
                        {
                            LogInfo($"CloseOrder result: Closed={item}");
                        }
                    }
                    break;
                default:
                    break;
            }


            // Wait for next action
            actionTimer.Change(Bot.ActionScriptInterval, 0);
        }
        private void NextActionTimerCall(object status)
        {
            actionTimer.Change(-1, -1);
            currentAction++;
            if (currentAction >= Actions.Length)
            {
                //script finished
                OnTestFinished(new EventArgs());            }
            else
            {
                Execute(Actions[currentAction]);                
            }
        }

        private void LogInfo(string message)
        {
            OnLog(new LogEventArgs(DateTime.UtcNow, $"Bot:[{Bot.Id}]-Thread[{Thread.CurrentThread.ManagedThreadId.ToString()}]", "info", message, null));
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
