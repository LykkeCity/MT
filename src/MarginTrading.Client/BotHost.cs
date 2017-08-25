using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

            StartBots();

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
            LogInfo("LoadSettings", $"Configuration Loaded: {_settingsFile}");
            
        }

        private void CreateEnvironment()
        {
            if (_settings.NumberOfUsers > _settings.Users.Length)
                throw new IndexOutOfRangeException("User array does not have enough users for requested NumberOfUsers ");

            Bots = new List<BotClient>();
            var userSettings = _settings.Users.OrderBy(x => x.Number).ToArray();
            for (int i = 0; i < _settings.NumberOfUsers; i++)
            {
                BotClient bot = new BotClient(userSettings[i]);                
                bot.LogEvent += Bot_LogEvent;
                Bots.Add(bot);
            }

        }

        private void StartBots()
        {
            foreach (var bot in Bots)
            {
                System.Threading.Tasks.Task.Run(async () => 
                {
                    await bot.Initialize(_settings.MTServerAddress, _settings.MTAuthorizationAddress);                    
                });

            }
        }

        List<BotScriptTest> RunningTests;
        public void RunActions()
        {
            RunningTests = new List<BotScriptTest>();
            foreach (var bot in Bots)
            {
                BotScriptTest test = new BotScriptTest(bot, _settings.Actions);
                test.LogEvent += Test_LogEvent;
                test.TestFinished += Test_TestFinished;
                RunningTests.Add(test);
                LogInfo($"Bot:[{bot.Id}]", $"Starting test for Bot: {bot.Email}");
                test.RunScriptAsync();
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
        public int SessionsPerUser { get; set; }
        public TestBotUserSettings[] Users { get; set; }
        public string[] Actions { get; set; }
    }
    class TestBotUserSettings
    {
        public int Number { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ClientInfo { get; set; }
        public string PartnerId { get; set; }
        public int ActionScriptInterval { get; set; }
        public int TransactionFrequency { get; set; }
        public int ReconnectFrequency { get; set; }
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
