using Flurl.Http;
using MarginTrading.Client.Settings;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.Client.Bot
{
    static class BotConsole
    {
        #region Vars
        private static BotHost _botHost;
        private static string _sessionLogFile;
        private static bool _isAutoRun;
        private static MtTradingBotSettings _settings;
        private static MtTradingBotTestSettings _testSettings;

        private static readonly object LogLock = new object();
        private static Queue<LogEventArgs> _logQueue;

        private static bool _isRunningTests;
        #endregion
        
        private static void LoadSettings(string testScriptFile)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            // If it's Development environment load local settings file, else load settings from Lykke.Settings
            TradingBotSettings mtSettings = (env == "Development") ?
                    config.Get<TradingBotSettings>() :
                    Lykke.SettingsReader.SettingsProcessor.Process<TradingBotSettings>(config["SettingsUrl"].GetStringAsync().Result);

            _settings = mtSettings.MtTradingBot;

            string script;
            if (!string.IsNullOrEmpty(_settings.TestFile))
                script = _settings.TestFile;
            else if (!string.IsNullOrEmpty(testScriptFile))
                script = testScriptFile;
            else
                throw new Exception("Invalid configuration file");

            FileInfo testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), script));
            if (!testFile.Exists)
                throw new FileNotFoundException($"Script file not found: {testFile.Name}");

            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddJsonFile(script, true, true)
                .AddEnvironmentVariables()
                .Build();

            _testSettings = config.Get<MtTradingBotTestSettings>();
            
            if (string.IsNullOrEmpty(_settings?.MTAuthorizationAddress) || string.IsNullOrEmpty(_settings.MTServerAddress))
                throw new Exception("Invalid configuration file");
            LogInfo("LoadSettings", $"Configuration Loaded: {script}");

        }
        internal static void StartBotHost(string configFile, bool autorun)
        {
            _isAutoRun = autorun;
            _sessionLogFile = Path.Combine(Path.GetTempPath(), "MTLOG_"+ DateTime.UtcNow.ToString("yyyyMMdd_HHmm") + ".log");

            LoadSettings(configFile);
            if (!string.IsNullOrEmpty(_settings.TestFile))
                _isAutoRun = true;

            _botHost = new BotHost();
            _botHost.LogEvent += Bot_LogEvent;
            _botHost.TestFinished += BotHost_TestFinished;
            _botHost.Start(_settings, _testSettings);

            LogInfo("BotConsole.StartBot", $"Log session file: {_sessionLogFile}");
            if (_isAutoRun)
                Run();
            string input;
            do
            {
                input = Console.ReadLine();
                switch (input)
                {
                    case "exit":
                        break;
                    case "bots":
                        ShowBots();
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    case "isalive":
                        IsAlive();
                        break;
                    case "initdata":
                        InitData();
                        break;
                    case "initaccounts":
                        InitAccounts();
                        break;
                    case "run":
                        Run();
                        break;
                    case "appinfo":
                        AppInfo();
                        break;
                    case "verifyemail":
                        VerifyEmail();
                        break;
                    case "register":
                        RegisterUser();
                        break;
                }

            } while (input != "exit");
            _botHost.Stop();
            _botHost.LogEvent -= Bot_LogEvent;
            _botHost.TestFinished -= BotHost_TestFinished;
            FlushLog();
        }                
        private static string RequestBot()
        {
            Console.Write("\tSelect bot [(#)bot number / (a)all]: ");
            var input = Console.ReadLine();
            if (input == "all" || input == "a")
                return "all";

            if (int.TryParse(input, out var botId))
            {

                var bot = _botHost.Bots.FirstOrDefault(x => x.Id == botId);
                if (bot == null)
                    return null;
                else
                    return input;


            }
            else
                return null;
        }

        #region Console Commands
        private static void Run()
        {
            if (_isRunningTests)
                LogInfo("Run", "Test is already running. Cannot run again");
            _isRunningTests = true;
            _botHost.RunScript();
        }

        private static async void AppInfo()
        {
            try
            {
                await MtUserHelper.ApplicationInfo(_settings.MTAuthorizationAddress);
            }
            catch (Exception ex)
            {
                LogError("MtUserHelper.EmailVerification", ex);
            }
        }
        private static async void VerifyEmail()
        {
            try
            {
                await MtUserHelper.EmailVerification("nem@dev.com", _settings.MTAuthorizationAddress);
            }
            catch (Exception ex)
            {
                LogError("MtUserHelper.EmailVerification", ex);
            }
        }
        private static async void RegisterUser()
        {
            Console.Write("Email: ");
            string email = Console.ReadLine();
            try
            {
                await MtUserHelper.EmailVerification(email, _settings.MTAuthorizationAddress);
            }
            catch (Exception ex)
            {
                LogError("MtUserHelper.EmailVerification", ex);
                return;
            }

            Console.Write("Password: ");
            string pass = Console.ReadLine();

            await MtUserHelper.Registration(email, pass, _settings.MTAuthorizationAddress);
        }
        private static void ShowBots()
        {
            Console.WriteLine(" ===== Bots ===== ");
            foreach (var bot in _botHost.Bots)
            {
                Console.WriteLine(" Bot Id: {0} > {1}", bot.Id, bot.Email);
            }
            Console.WriteLine(" ===== ==== ===== ");
        }
        private static void ShowHelp()
        {
            Console.WriteLine(" ===== HELP ===== ");
            Console.WriteLine(" bots - Show active bots ");
            Console.WriteLine(" isalive - Perform IsAlive call for 1 or all bots ");
            Console.WriteLine(" initdata - Perform InitData call for 1 or all bots ");
            Console.WriteLine(" initaccounts - Perform InitAccounts call for 1 or all bots ");
            Console.WriteLine(" run - Run actions script ");
            Console.WriteLine(" exit - Stops bot application ");
            Console.WriteLine(" ===== ==== ===== ");
        }
        private static void IsAlive()
        {
            string botid = RequestBot();
            if (botid == null)
                Console.WriteLine("Invalid bot id");
            else if (botid == "all")
            {
                foreach (var bot in _botHost.Bots)
                {
                    Task.Run(() => bot.IsAlive());
                }
            }
            else
            {   
                var bot = _botHost.Bots.FirstOrDefault(x => x.Id.ToString() == botid);
                bot?.IsAlive();
            }
        }
        private static void InitData()
        {
            string botid = RequestBot();
            switch (botid)
            {
                case null:
                    Console.WriteLine("Invalid bot id");
                    break;
                case "all":
                    foreach (var b in _botHost.Bots)
                    {
                        Task.Run(() => b.InitData());
                    }
                    break;
                default:
                    var bot = _botHost.Bots.FirstOrDefault(x => x.Id.ToString() == botid);
                    bot?.InitData();
                    break;
            }
        }
        private static void InitAccounts()
        {
            string botid = RequestBot();
            if (botid == null)
                Console.WriteLine("Invalid bot id");
            else if (botid == "all")
            {
                foreach (var bot in _botHost.Bots)
                {
                    Task.Run(() => bot.InitAccounts());
                }
            }
            else
            {
                var bot = _botHost.Bots.FirstOrDefault(x => x.Id.ToString() == botid);
                bot?.InitAccounts();
            }
        }
        #endregion
        
        #region Logging
        private static void LogInfo(string origin, string message)
        {
            Log(new LogEventArgs(DateTime.UtcNow, origin, "info", message, null));
        }
      
        private static void LogError(string origin, Exception  error)
        {
            Log(new LogEventArgs(DateTime.UtcNow, origin, "error", error.Message, error));
        }
                
        private static void Log(LogEventArgs e)
        {
            LogEventArgs[] currentLogBuffer = null;

            lock (LogLock)
            {
                if (_logQueue == null)
                    _logQueue = new Queue<LogEventArgs>();

                _logQueue.Enqueue(e);
                if (_logQueue.Count >= 64)
                {
                    currentLogBuffer = _logQueue.ToArray();
                    _logQueue.Clear();
                }
            }            

            var msg = $"{e.Date:HH:mm:ss.fff};{e.Origin};{e.Type};{e.Message}";
            if (e.Exception != null)
                msg += $"\n\r{e.Exception.GetBaseException().Message}\n\rSTACK:[{e.Exception.GetBaseException().StackTrace}]";
            Console.WriteLine(msg);

            if (currentLogBuffer != null)
                FlushLog(currentLogBuffer);
        }
        private static void FlushLog()
        {
            LogEventArgs[] buffer;
            lock (LogLock)
            {
                buffer = _logQueue.ToArray();
                _logQueue.Clear();
            }
            FlushLog(buffer);
        }
        private static void FlushLog(LogEventArgs[] logBuffer)
        {          
            using (var fs = new FileStream(_sessionLogFile, FileMode.Append, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
            {
                foreach (var logitem in logBuffer)
                {
                    sw.WriteLine("{0};{1};{2};{3}", logitem.Date.ToString("HH:mm:ss.fff"), logitem.Origin, logitem.Type, logitem.Message);
                    if (logitem.Exception != null)
                        sw.WriteLine($"{logitem.Exception.GetBaseException().Message}\n\rSTACK:[{logitem.Exception.GetBaseException().StackTrace}]");
                }                
            }
        }
        #endregion

        #region Event Handlers
        private static void BotHost_TestFinished(object sender, EventArgs e)
        {
            FlushLog();
            _isRunningTests = false;
        }
        private static void Bot_LogEvent(object sender, LogEventArgs e)
        {
            Log(e);
        }
        #endregion
    }
}
