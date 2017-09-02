using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Client
{
    

    class BotHost
    {
        public event EventHandler<LogEventArgs> LogEvent;

        private string _settingsFile;
        private TestBotSettings _settings;

        public List<BotClient> Bots { get; private set; }

        public BotHost()
        {
            _settingsFile = "appsettings.dev.json";
        }
        public void Start(string settingsFile  = null)
        {
            if (settingsFile != null)
                _settingsFile = settingsFile;
            try { LoadSettings(); }
            catch (Exception ex01)
            {
                LogError("LoadSettings", ex01);
                throw;
            }

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


        private void LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_settingsFile, true, true)                
                .Build();
            _settings = config.Get<TestBotSettings>();
            if (_settings == null || string.IsNullOrEmpty(_settings.MTAuthorizationAddress) || string.IsNullOrEmpty(_settings.MTServerAddress))
                throw new ArgumentNullException("Invalid configuration file");
            LogInfo("BotHost.LoadSettings", $"Configuration Loaded: {_settingsFile}");
            
        }

        private void CreateEnvironment()
        {
            
            Bots = new List<BotClient>();

            if (string.IsNullOrEmpty(_settings.UsersFile))
            {
                if (_settings.NumberOfUsers > _settings.Users.Length)
                    throw new IndexOutOfRangeException("User array does not have enough users for requested NumberOfUsers ");

                // Load Users Json Array
                var userSettings = _settings.Users.OrderBy(x => x.Number).ToArray();
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
                            await MtUserHelper.Registration(bot.Email, bot.Password);
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
        public void RunActions()
        {
            RunningTests = new List<BotScriptTest>();
            foreach (var bot in Bots)
            {
                if (bot.Initialized)
                {
                    BotScriptTest test = new BotScriptTest(bot, _settings.Actions);
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
        private void OnLog(object sender, LogEventArgs e)
        {
            LogEvent?.Invoke(sender, e);
        }

        private void Bot_LogEvent(object sender, LogEventArgs e)
        {
            OnLog(sender,e);
        }

        
    }
    class TestBotSettings
    {
        public string MTServerAddress { get; set; }
        public string MTAuthorizationAddress { get; set; }
        public int NumberOfUsers { get; set; }
        public int ActionScriptInterval { get; set; }
        public int TransactionFrequencyMin { get; set; }
        public int TransactionFrequencyMax { get; set; }
        public string UsersFile { get; set; } = null;
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
