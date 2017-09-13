using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace MarginTrading.Client
{


    class BotHost
    {
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler TestFinished;
                
        private MtTradingBotSettings _settings;
        private MtTradingBotTestSettings _testSettings;

        public List<BotClient> Bots { get; private set; }        
                
        public void Start(MtTradingBotSettings settings, MtTradingBotTestSettings testSettings)
        {
            _settings = settings;
            _testSettings = testSettings;

            try { CreateEnvironment(); }
            catch (Exception ex01)
            {
                LogError("CreateEnvironment", ex01);
                throw;
            }

            Task.Run(async () => await StartBots())
                .Wait();
            

        }
        public void Stop()
        {
            if (Bots != null && Bots.Count > 0)
            {
                foreach (var bot  in Bots)
                {
                    bot.Dispose();
                }                
            }
        }


       

        private void CreateEnvironment()
        {
            
            Bots = new List<BotClient>();
                        
            if (string.IsNullOrEmpty(_settings.UsersFile))
            {
                if (_settings.NumberOfUsers > _testSettings.Users.Length)
                    throw new IndexOutOfRangeException("User array does not have enough users for requested NumberOfUsers ");

                // Load Users Json Array
                var userSettings = _testSettings.Users.OrderBy(x => x.Number).ToArray();
                for (int i = 0; i < _settings.NumberOfUsers; i++)
                {
                    BotClient bot = new BotClient(userSettings[i]);
                    bot.LogEvent += Bot_LogEvent;
                    Bots.Add(bot);
                }
            }
            else
            {
                // Load Users from CSV File
                FileInfo file = new FileInfo(_settings.UsersFile);
                if (!file.Exists)
                    throw new FileNotFoundException(file.FullName);

                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs))
                {
                    //Read Header
                    string header = sr.ReadLine();
                    int ctr = 0;
                    do
                    {
                        if (ctr >= _settings.NumberOfUsers)
                            break;
                        // read csv line
                        string line = sr.ReadLine();
                        string[] values = line.Split(';');
                        string email = values[0];
                        string pass = values[1];

                        BotClient bot = new BotClient(new TestBotUserSettings() { Number = ++ctr, Email = email, Password = pass });
                        bot.LogEvent += Bot_LogEvent;
                        Bots.Add(bot);

                    } while (!sr.EndOfStream);
                }
            }
        }

        private async Task StartBots()
        {
            foreach (var bot in Bots)
            {

                int tryCount = 0;
                while (true)
                {
                    if (tryCount > 3)
                        break;
                    tryCount++;

                    bool create = false;
                    try
                    {
                        await bot.Initialize(_settings.MTServerAddress, _settings.MTAuthorizationAddress, _settings.ActionScriptInterval, _settings.TransactionFrequencyMin, _settings.TransactionFrequencyMax);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Invalid username or password")
                        {
                            create = true;
                        }
                    }
                    if (create)
                    {
                        LogInfo("StartBots", $"Creating user: {bot.Email}");
                        try
                        {
                            await MtUserHelper.Registration(bot.Email, bot.Password, _settings.MTAuthorizationAddress);
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ex01)
                        {
                            LogError("MtUserHelper.Registration", ex01);
                            break;
                        }
                    }
                }
                
            }
        }

        List<BotScriptTest> RunningTests;
        List<BotScriptTest> FinishedTests;
        public void RunActions()
        {
            RunningTests = new List<BotScriptTest>();
            FinishedTests = new List<BotScriptTest>();
            foreach (var bot in Bots)
            {
                if (bot.Initialized)
                {
                    BotScriptTest test = new BotScriptTest(bot, _testSettings.Actions);
                    test.LogEvent += Test_LogEvent;
                    test.TestFinished += Test_TestFinished;
                    RunningTests.Add(test);
                    LogInfo($"Bot:[{bot.Id}]", $"Starting test for Bot: {bot.Email}");
                    test.RunScriptAsync();
                }
                else
                {
                    LogWarning($"Bot:[{bot.Id}]", $"Bot initialization failed: {bot.Email}. Not running tests");
                }
            }
        }
                
        private void Test_LogEvent(object sender, LogEventArgs e)
        {
            OnLog(sender, e);
        }
        private void Test_TestFinished(object sender, EventArgs e)
        {
            BotScriptTest sdr = sender as BotScriptTest;
            LogInfo($"Bot:[{sdr.Bot.Id}]", $"Test Finished! Bot: {sdr.Bot.Email}");
            RunningTests.Remove(sdr);
            FinishedTests.Add(sdr);
            if (RunningTests.Count == 0)
            {
                string[] methods = FinishedTests.First().Operations.Select(x => x.Operation).ToArray();
                LogInfo("BotHost", ";BOT;OPERATION;COUNT;AVERAGE");

                List<OperationResult> TotalOperations = new List<OperationResult>();
                foreach (var item in FinishedTests)
                {
                    TotalOperations.AddRange(item.Operations);
                    var grouped = item.Operations.GroupBy(x => x.Operation);                    
                    foreach (var group in grouped)
                    {
                        string Line = $";{item.Bot.Id};{group.Key};{group.Count()};{group.Average(x => x.Duration.TotalSeconds)}";
                        LogInfo("BotHost", Line);
                    }                    
                }

                LogInfo("BotHost", " === Total Averages === ");
                LogInfo("BotHost", "OPERATION;COUNT;AVERAGE");
                var totalgrouped = TotalOperations.GroupBy(x => x.Operation);
                foreach (var group in totalgrouped)
                {
                    string Line = $";{group.Key};{group.Count()};{group.Average(x => x.Duration.TotalSeconds)}";
                    LogInfo("BotHost", Line);
                }
                LogInfo("BotHost", " === Total Averages === ");

                OnTestFinished(this, new EventArgs());
            }
        }

        private void LogInfo(string origin, string message)
        {
            OnLog(this, new LogEventArgs(DateTime.UtcNow, origin, "info", message, null));
        }
        private void LogWarning(string origin, string message)
        {
            OnLog(this, new LogEventArgs(DateTime.UtcNow, origin, "warning", message, null));
        }
        private void LogError(string origin, Exception error)
        {
            OnLog(this, new LogEventArgs(DateTime.UtcNow, origin, "error", error.Message, error));
        }
        private void OnTestFinished(object sender, EventArgs e)
        {
            TestFinished?.Invoke(sender, e);
        }
        private void OnLog(object sender, LogEventArgs e)
        {
            LogEvent?.Invoke(sender, e);
        }

        private void Bot_LogEvent(object sender, LogEventArgs e)
        {
            OnLog(sender,e);
        }

        
    }
    class TradingBotSettings
    {        
            public MtTradingBotSettings MtTradingBot { get; set; }
    }
    class MtTradingBotSettings
    {
        public string MTServerAddress { get; set; }
        public string MTAuthorizationAddress { get; set; }
        public int NumberOfUsers { get; set; }
        public int ActionScriptInterval { get; set; }
        public int TransactionFrequencyMin { get; set; }
        public int TransactionFrequencyMax { get; set; }
        public string UsersFile { get; set; } = null;
        public string TestFile { get; set; } = null;
    }
    class MtTradingBotTestSettings
    {        
        public TestBotUserSettings[] Users { get; set; }
        public string[] Actions { get; set; }
    }
    class TestBotUserSettings
    {
        public int Number { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }        
    }
    
    
    public class LogEventArgs : EventArgs
    {
        DateTime date;
        string origin;
        string type;
        string message;
        Exception exception;

        public LogEventArgs(DateTime date, string origin, string type, string message, Exception exception)
        {
            this.date = date;
            this.origin = origin;
            this.type = type;
            this.message = message;
            this.exception = exception;
        }

        public DateTime Date { get => date; }
        public string Origin { get => origin; }
        public string Type { get => type; }
        public string Message { get => message; }
        public Exception Exception { get => exception; }
    }
}
