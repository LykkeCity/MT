using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using WampSharp.V2.Rpc;

namespace MarginTrading.Common.Wamp
{
    public interface IRpcMtBackend
    {
        [WampProcedure("init.data")]
        Task<InitDataBackendResponse> InitData(ClientIdBackendRequest request);

        [WampProcedure("init.chartdata")]
        InitChartDataBackendResponse InitChardData();

        [WampProcedure("init.accounts")]
        MarginTradingAccountBackendContract[] InitAccounts(ClientIdBackendRequest request);

        [WampProcedure("init.accountinstruments")]
        InitAccountInstrumentsBackendResponse AccountInstruments(ClientIdBackendRequest request);

        [WampProcedure("init.graph")]
        InitChartDataBackendResponse InitGraph();

        [WampProcedure("init.availableassets")]
        string[] InitAvailableAssets(ClientIdBackendRequest request);

        [WampProcedure("init.assets")]
        MarginTradingAssetBackendContract[] InitAssets();

        [WampProcedure("account.history")]
        Task<AccountHistoryBackendResponse> GetAccountHistory(AccountHistoryBackendRequest request);

        [WampProcedure("account.history.new")]
        Task<AccountNewHistoryBackendResponse> GetHistory(AccountHistoryBackendRequest request);

        [WampProcedure("order.place")]
        Task<OpenOrderBackendResponse> PlaceOrder(OpenOrderBackendRequest request);

        [WampProcedure("order.close")]
        MtBackendResponse<bool> CloseOrder(CloseOrderBackendRequest request);

        [WampProcedure("order.list")]
        OrderBackendContract[] GetOpenPositions(ClientIdBackendRequest request);

        [WampProcedure("order.positions")]
        ClientOrdersBackendResponse GetClientOrders(ClientIdBackendRequest request);

        [WampProcedure("order.changeLimits")]
        MtBackendResponse<bool> ChangeOrderLimits(ChangeOrderLimitsBackendRequest request);

        [WampProcedure("orderbooks")]
        OrderbooksBackendResponse GetOrderBooks();

        [WampProcedure("ping")]
        MtBackendResponse<string> Ping();
    }
}
