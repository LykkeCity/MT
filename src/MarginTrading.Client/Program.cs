using System;

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
                BotConsole.StartBot(TestBotSettingsFile);
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
