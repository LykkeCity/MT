namespace MarginTrading.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var client = new MtClient();

            client.Connect(ClientEnv.Local);

            client.IsAlive();
            client.InitData().Wait();
            //client.InitAccounts();
            //client.AccountInstruments();
            //client.InitGraph();

            //client.AccountDeposit().Wait();
            //client.AccountWithdraw();
            //client.SetActiveAccount();
            //client.GetAccountHistory();
            //client.GetHistory();

            //client.PlaceOrder().Wait();
            //client.CloseOrder(true).Wait();
            //client.CancelOrder();
            //client.GetAccountOpenPositions().Wait();
            client.GetOpenPositions().Wait();
            //client.GetClientOrders();
            //client.ChangeOrderLimits();

            //client.Prices();
            //client.UserUpdates();
        }
    }
}
