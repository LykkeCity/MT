using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Common.Documentation;
using WampSharp.V2.Rpc;
using IsAliveResponse = MarginTrading.Common.ClientContracts.IsAliveResponse;

namespace MarginTrading.Common.Wamp
{
    public interface IRpcMtFrontend
    {
        [WampProcedure("is.alive")]
        [DocMe(Name = "is.alive", Description = "Checks service isAlive")]
        IsAliveResponse IsAlive();

        [WampProcedure("init.data")]
        [DocMe(Name = "init.data", Description = "Gets init data: client accounts and trading conditions")]
        Task<InitDataLiveDemoClientResponse> InitData(string token);

        [WampProcedure("init.accounts")]
        [DocMe(Name = "init.accounts", Description = "Gets client accounts")]
        Task<InitAccountsLiveDemoClientResponse> InitAccounts(string token);

        [WampProcedure("init.accountinstruments")]
        [DocMe(Name = "init.accountinstruments", Description = "Gets trading conditions")]
        Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string token = null);

        [WampProcedure("init.graph")]
        [DocMe(Name = "init.graph", Description = "Gets data for micrographics")]
        Task<InitChartDataClientResponse> InitGraph(string token = null, string[] assetIds = null);

        [WampProcedure("account.history")]
        [DocMe(Name = "account.history", Description = "Gets account history", InputType = typeof(AccountHistoryRpcClientRequest))]
        Task<AccountHistoryClientResponse> GetAccountHistory(string requestJson);

        [WampProcedure("account.history.new")]
        [DocMe(Name = "account.history.new", Description = "Gets account history (different format)", InputType = typeof(AccountHistoryRpcClientRequest))]
        Task<AccountHistoryItemClient[]> GetHistory(string requestJson);

        [WampProcedure("order.place")]
        [DocMe(Name = "order.place", Description = "Places order", InputType = typeof(OpenOrderRpcClientRequest))]
        Task<MtClientResponse<OrderClientContract>> PlaceOrder(string requestJson);

        [WampProcedure("order.close")]
        [DocMe(Name = "order.close", Description = "Close order", InputType = typeof(CloseOrderRpcClientRequest))]
        Task<MtClientResponse<bool>> CloseOrder(string requestJson);

        [WampProcedure("order.cancel")]
        [DocMe(Name = "order.cancel", Description = "Cancel order", InputType = typeof(CloseOrderRpcClientRequest))]
        Task<MtClientResponse<bool>> CancelOrder(string requestJson);

        [WampProcedure("order.list")]
        [DocMe(Name = "order.list", Description = "Gets client open positions")]
        Task<ClientOrdersLiveDemoClientResponse> GetOpenPositions(string token);

        [WampProcedure("order.account.list")]
        [DocMe(Name = "order.account.list", Description = "Gets client account open positions", InputType = typeof(AccountTokenClientRequest))]
        Task<OrderClientContract[]> GetAccountOpenPositions(string requestJson);

        [WampProcedure("order.positions")]
        Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string token);

        [WampProcedure("order.changeLimits")]
        [DocMe(Name = "order.changeLimits", Description = "Sets order limits", InputType = typeof(ChangeOrderLimitsRpcClientRequest))]
        Task<MtClientResponse<bool>> ChangeOrderLimits(string requestJson);

        [WampProcedure("orderbooks")]
        Task<OrderBookClientContract> GetOrderBook(string instrument);
    }
}
