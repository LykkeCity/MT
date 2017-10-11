using MarginTrading.Client.Bot;
using System;

namespace MarginTrading.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var testBot = false;
            string testBotSettingsFile = null;
            var autorun = false;            
            for (var i = 0; i< args.Length; i++)
            {

                if (args[i].ToLower() == "-h" || args[i].ToLower() == "--help")
                {
                    Console.WriteLine("-h\t--help Show this help");
                    Console.WriteLine("-b\t--bot Run test bot. Usage: -b [json configuration file]");
                    Console.ReadKey();
                    return;
                }               
                if (args[i].ToLower() == "-a" || args[i].ToLower() == "--autorun")
                {
                    autorun = true;                    
                }
                if (args[i].ToLower() == "-b" || args[i].ToLower() == "--bot")
                {
                    testBot = true;
                    if (args.Length > i + 1 && !args[i + 1].StartsWith('-'))
                    {
                        try { testBotSettingsFile = args[++i]; }
                        catch { testBotSettingsFile = ""; }
                    }
                }
            }
            if (testBot)
            {
                BotConsole.StartBotHost(testBotSettingsFile, autorun);
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
    }
}
