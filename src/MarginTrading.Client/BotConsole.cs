using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Client
{
    static class BotConsole
    {
        static BotHost botHost;
        static string sessionLogFile;        
        internal static void StartBot(string configFile, bool autorun)
        {
            
            sessionLogFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MTLOG_"+ DateTime.UtcNow.ToString("yyyyMMdd_HHmm") + ".log");
            botHost = new BotHost();
            botHost.LogEvent += Bot_LogEvent;
            botHost.Start(configFile);
            LogInfo("BotConsole.StartBot", $"Log session file: {sessionLogFile}");
            if (autorun)
                Run();
            string input = "";
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
                        MtUserHelper.ApplicationInfo();
                        break;
                    case "verifyemail":
                        MtUserHelper.EmailVerification("nem@dev.com");
                        break;
                    case "register":
                        RegisterUser();
                        break;
                    default:
                        Console.WriteLine("Unknown command [{0}]", input);
                        break;
                }

            } while (input != "exit");
            botHost.Stop();
            botHost.LogEvent -= Bot_LogEvent;
            FlushLog();
        }

        private async static void RegisterUser()
        {
            Console.Write("Email: ");
            string email = Console.ReadLine();
            try
            {
                MtUserHelper.EmailVerification(email);
            }
            catch (Exception ex)
            {
                LogError("MtUserHelper.EmailVerification", ex);
                return;
            }

            Console.Write("Password: ");
            string pass = Console.ReadLine();

            await MtUserHelper.Registration(email,pass);
        }

        private static void Run()
        {
            botHost.RunActions();
        }

        private static void ShowBots()
        {
            Console.WriteLine(" ===== Bots ===== ");
            foreach (var bot in botHost.Bots)
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
            string botid = GetBot();
            if (botid == null)
                Console.WriteLine("Invalid bot id");
            else if (botid == "all")
            {
                foreach (var bot in botHost.Bots)
                {
                    Task.Run(() => bot.IsAlive());
                }
            }
            else
            {
                var bot = botHost.Bots.Where(x => x.Id.ToString() == botid).FirstOrDefault();
                bot.IsAlive();
            }
        }

        private static void InitData()
        {
            string botid = GetBot();
            if (botid == null)
                Console.WriteLine("Invalid bot id");
            else if (botid == "all")
            {
                foreach (var bot in botHost.Bots)
                {
                    Task.Run(() => bot.InitData());
                }
            }
            else
            {
                var bot = botHost.Bots.Where(x => x.Id.ToString() == botid).FirstOrDefault();
                bot.InitData();
            }
        }
        private static void InitAccounts()
        {
            string botid = GetBot();
            if (botid == null)
                Console.WriteLine("Invalid bot id");
            else if (botid == "all")
            {
                foreach (var bot in botHost.Bots)
                {
                    Task.Run(() => bot.InitAccounts());
                }
            }
            else
            {
                var bot = botHost.Bots.Where(x => x.Id.ToString() == botid).FirstOrDefault();
                bot.InitAccounts();
            }
        }


        private static string GetBot()
        {
            Console.Write("\tSelect bot [(#)bot number / (a)all]: ");
            var input = Console.ReadLine();
            int botId = 0;
            if (input == "all" || input == "a")
                return "all";
            else if (int.TryParse(input, out botId))
            {

                var bot = botHost.Bots.Where(x => x.Id == botId);
                if (bot == null)
                    return null;
                else
                    return input;


            }
            else
                return null;
        }

        private static void Bot_LogEvent(object sender, LogEventArgs e)
        {
            Log(e);
        }

        private static void LogInfo(string origin, string message)
        {
            Log(new LogEventArgs(DateTime.UtcNow, origin, "info", message, null));
        }
        private static void LogWarning(string origin, string message)
        {
            Log(new LogEventArgs(DateTime.UtcNow, origin, "warning", message, null));
        }
        private static void LogError(string origin, Exception  error)
        {
            Log(new LogEventArgs(DateTime.UtcNow, origin, "error", error.Message, error));
        }

        private static object LogLock = new object();
        static Queue<LogEventArgs> LogQueue = null;

        private static void Log(LogEventArgs e)
        {
            if (LogQueue == null)
                LogQueue = new Queue<LogEventArgs>();
            LogEventArgs[] currentLogBuffer = null;

            lock (LogLock)
            {
                LogQueue.Enqueue(e);
                if (LogQueue.Count >= 64)
                {
                    currentLogBuffer = LogQueue.ToArray();
                    LogQueue.Clear();
                }
            }            

            string msg = string.Format("{0};{1};{2};{3}", e.Date.ToString("HH:mm:ss.fff"), e.Origin, e.Type, e.Message);
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
                buffer = LogQueue.ToArray();
                LogQueue.Clear();
            }
            FlushLog(buffer);
        }
        private static void FlushLog(LogEventArgs[] logBuffer)
        {          
            using (var fs = new System.IO.FileStream(sessionLogFile, System.IO.FileMode.Append, System.IO.FileAccess.Write))
            using (var sw = new System.IO.StreamWriter(fs))
            {
                foreach (var logitem in logBuffer)
                {
                    sw.WriteLine("{0};{1};{2};{3}", logitem.Date.ToString("HH:mm:ss.fff"), logitem.Origin, logitem.Type, logitem.Message);
                    if (logitem.Exception != null)
                        sw.WriteLine($"{logitem.Exception.GetBaseException().Message}\n\rSTACK:[{logitem.Exception.GetBaseException().StackTrace}]");
                }                
            }
        }
    }
}
