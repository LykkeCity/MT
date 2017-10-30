using System.Threading.Tasks;
using MarginTrading.Contract.ClientContracts;
using WampSharp.V2.Rpc;

namespace MarginTrading.Client.Wamp
{
    public interface IRpcMtFrontend
    {
        [WampProcedure("is.alive")]
        IsAliveResponse IsAlive();

        [WampProcedure("init.data")]
        Task<InitDataLiveDemoClientResponse> InitData(string token);

        [WampProcedure("init.accounts")]
        Task<InitAccountsLiveDemoClientResponse> InitAccounts(string token);

        [WampProcedure("init.accountinstruments")]
        Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string token = null);

        [WampProcedure("init.graph")]
        Task<InitChartDataClientResponse> InitGraph(string token = null, string[] assetIds = null);

        [WampProcedure("account.history")]
        Task<AccountHistoryClientResponse> GetAccountHistory(string requestJson);

        [WampProcedure("account.history.new")]
        Task<AccountHistoryItemClient[]> GetHistory(string requestJson);

        [WampProcedure("order.place")]
        Task<MtClientResponse<OrderClientContract>> PlaceOrder(string requestJson);

        [WampProcedure("order.close")]
        Task<MtClientResponse<bool>> CloseOrder(string requestJson);

        [WampProcedure("order.cancel")]
        Task<MtClientResponse<bool>> CancelOrder(string requestJson);

        [WampProcedure("order.list")]
        Task<ClientOrdersLiveDemoClientResponse> GetOpenPositions(string token);

        [WampProcedure("order.account.list")]
        Task<OrderClientContract[]> GetAccountOpenPositions(string requestJson);

        [WampProcedure("order.positions")]
        Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string token);

        [WampProcedure("order.changeLimits")]
        Task<MtClientResponse<bool>> ChangeOrderLimits(string requestJson);

        [WampProcedure("orderbooks")]
        Task<OrderBookClientContract> GetOrderBook(string instrument);
    }
}
