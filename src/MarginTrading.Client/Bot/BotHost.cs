using MarginTrading.Client.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.Client.Bot
{
    class BotHost
    {
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler TestFinished;
        
        #region vars
        private MtTradingBotSettings _settings;
        private MtTradingBotTestSettings _testSettings;

        private List<BotTest> _runningTests;
        private List<BotTest> _finishedTests;
        #endregion

        #region Properties
        public List<BotClient> Bots { get; private set; }
        #endregion

        #region Methods
        public void Start(MtTradingBotSettings settings, MtTradingBotTestSettings testSettings)
        {
            _settings = settings;
            _testSettings = testSettings;

            // Create Bots
            try { CreateBots(); }
            catch (Exception ex01)
            {
                LogError("CreateEnvironment", ex01);
                throw;
            }

            // Initialize Bots
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
        
        private void CreateBots()
        {
            
            Bots = new List<BotClient>();
                        
            if (string.IsNullOrEmpty(_settings.UsersFile))
            {
                if (_settings.NumberOfUsers > _testSettings.Users.Length)
                    throw new IndexOutOfRangeException("User array does not have enough users for requested NumberOfUsers ");

                // Load Users Json Array
                var userSettings = _testSettings.Users.OrderBy(x => x.Number).ToArray();
                for (var i = 0; i < _settings.NumberOfUsers; i++)
                {
                    var bot = new BotClient(userSettings[i]);
                    bot.LogEvent += Bot_LogEvent;
                    Bots.Add(bot);
                }
            }
            else
            {
                // Load Users from CSV File
                var file = new FileInfo(_settings.UsersFile);
                if (!file.Exists)
                    throw new FileNotFoundException(file.FullName);

                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs))
                {
                    //Read Header
                    sr.ReadLine();
                    var ctr = 0;
                    do
                    {
                        if (ctr >= _settings.NumberOfUsers)
                            break;
                        // read csv line
                        var line = sr.ReadLine();
                        var values = line.Split(';');
                        var email = values[0];
                        var pass = values[1];

                        var bot = new BotClient(new TestBotUserSettings() { Number = ++ctr, Email = email, Password = pass });
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
                var tryCount = 0;
                while (true)
                {
                    if (tryCount >= 3)
                        break;
                    tryCount++;

                    var createUser = false;
                    try
                    {
                        await bot.Initialize(_settings.MTServerAddress, _settings.MTAuthorizationAddress, _settings.ActionScriptInterval, _settings.TransactionFrequencyMin, _settings.TransactionFrequencyMax);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Invalid username or password")
                            createUser = true;
                    }
                    if (createUser)
                    {
                        LogInfo("StartBots", $"Creating user: {bot.Email}");
                        try
                        {
                            await MtUserHelper.Registration(bot.Email, bot.Password, _settings.MTAuthorizationAddress);
                            LogInfo("StartBots", $"User {bot.Email} created successfully");
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

        public void RunScript()
        {
            _runningTests = new List<BotTest>();
            _finishedTests = new List<BotTest>();
            foreach (var bot in Bots)
            {
                if (bot.Initialized)
                {
                    var test = new BotTest(bot, _testSettings.Actions);
                    test.LogEvent += Test_LogEvent;
                    test.TestFinished += Test_TestFinished;
                    _runningTests.Add(test);
                    LogInfo($"Bot:[{bot.Id}]", $"Starting test for Bot: {bot.Email}");
                    test.RunScriptAsync();
                }
                else
                {
                    LogWarning($"Bot:[{bot.Id}]", $"Bot initialization failed: {bot.Email}. Not running tests");
                }
            }
        }

        private void PrintSummary()
        {
            LogInfo("BotHost", ";BOT;OPERATION;COUNT;AVERAGE");

            var totalOperations = new List<OperationResult>();
            foreach (var item in _finishedTests)
            {
                totalOperations.AddRange(item.Operations);
                var grouped = item.Operations.GroupBy(x => x.Operation);
                foreach (var group in grouped)
                {
                    var line = $";{item.Bot.Id};{group.Key};{group.Count()};{group.Average(x => x.Duration.TotalSeconds)}";
                    LogInfo("BotHost", line);
                }
            }

            LogInfo("BotHost", " === Total Averages === ");
            LogInfo("BotHost", "OPERATION;COUNT;AVERAGE");
            var totalgrouped = totalOperations.GroupBy(x => x.Operation);
            foreach (var group in totalgrouped)
            {
                var line = $";{group.Key};{group.Count()};{group.Average(x => x.Duration.TotalSeconds)}";
                LogInfo("BotHost", line);
            }
            LogInfo("BotHost", " === Total Averages === ");
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
        #endregion

        #region Event Raising
        private void OnTestFinished(object sender, EventArgs e)
        {
            TestFinished?.Invoke(sender, e);
        }
        private void OnLog(object sender, LogEventArgs e)
        {
            LogEvent?.Invoke(sender, e);
        }
        #endregion

        #region Event Handlers
        private void Test_LogEvent(object sender, LogEventArgs e)
        {
            OnLog(sender, e);
        }
        private void Bot_LogEvent(object sender, LogEventArgs e)
        {
            OnLog(sender, e);
        }
        private void Test_TestFinished(object sender, EventArgs e)
        {
            var sdr = sender as BotTest;
            LogInfo($"Bot:[{sdr?.Bot.Id}]", $"Test Finished! Bot: {sdr?.Bot.Email}");
            _runningTests.Remove(sdr);
            _finishedTests.Add(sdr);
            if (_runningTests.Count == 0)
            {
                PrintSummary();
                OnTestFinished(this, new EventArgs());
            }
        }
        #endregion

    }
    
        
  
    
    
    
}
