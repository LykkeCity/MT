using System;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool TestBot = false;
            string TestBotSettingsFile = null;
            for (int i = 0; i< args.Length; i++)
            {

                if (args[i].ToLower() == "-h" || args[i].ToLower() == "--help")
                {
                    Console.WriteLine("-h\t--help Show this help");
                    Console.WriteLine("-b\t--bot Run test bot. Usage: -b [json configuration file]");
                    Console.ReadKey();
                    return;
                }
                if (args[i].ToLower() == "-b" || args[i].ToLower() == "--bot")
                {
                    TestBot = true;
                    try { TestBotSettingsFile = args[++i]; }
                    catch { TestBotSettingsFile = ""; }
                }
            }
            if (TestBot)
            {
                StartBot(TestBotSettingsFile);
            }
            else
            {
                for (int i = 0; i < 1; i++)
                {
                    var client = new MtClient();

                    client.Connect(ClientEnv.Dev);

                    client.IsAlive();
                    //client.InitData().Wait();
                    //client.InitAccounts();
                    //client.AccountInstruments();
                    //client.InitGraph().Wait();

                    //client.AccountDeposit().Wait();
                    //client.AccountWithdraw();
                    //client.SetActiveAccount();
                    //client.GetAccountHistory();
                    //client.GetHistory();

                    //client.PlaceOrder().Wait();
                    client.CloseOrder(true).Wait();
                    //client.CancelOrder();
                    //client.GetAccountOpenPositions().Wait();
                    //client.GetOpenPositions().Wait();
                    //client.GetClientOrders();
                    //client.ChangeOrderLimits();

                    client.Prices("EURUSD");
                    //client.UserUpdates();

                    client.Close();
                }
            }
        }
        static BotHost botHost;
        private static void StartBot(string configFile)
        {
            botHost = new BotHost();
            botHost.LogEvent += Bot_LogEvent;
            botHost.Start(configFile);
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
                    default:
                        Console.WriteLine("Unknown command [{0}]", input);
                        break;
                }

            } while (input != "exit");
            botHost.Stop();
            botHost.LogEvent -= Bot_LogEvent;
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
                    Task.Run(() =>  bot.InitData());
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
            Console.WriteLine("{0};{1};{2};{3}", e.Date.ToString("HH:mm:ss.fff"), e.Origin, e.Type, e.Message);
            if (e.Exception != null)
                Console.WriteLine(e.Exception.GetBaseException().Message + "\n\r" + e.Exception.GetBaseException().StackTrace);
        }
    }
}
