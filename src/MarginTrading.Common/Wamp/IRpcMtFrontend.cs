using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
using WampSharp.V2.Rpc;

namespace MarginTrading.Common.Wamp
{
    public interface IRpcMtFrontend
    {
        [WampProcedure("init.data")]
        Task<InitDataLiveDemoClientResponse> InitData(string token);

        [WampProcedure("init.accounts")]
        Task<InitAccountsLiveDemoClientResponse> InitAccounts(string token);

        [WampProcedure("init.accountinstruments")]
        Task<InitAccountInstrumentsLiveDemoClientResponse> AccountInstruments(string token);

        [WampProcedure("init.graph")]
        Task<InitChartDataLiveDemoClientResponse> InitGraph();

        [WampProcedure("init.orderbook")]
        Task<AggregatedOrderbookLiveDemoClientContract> InitOrderBook(string instrument);

        [WampProcedure("account.deposit")]
        Task<MtClientResponse<bool>> AccountDeposit(string requestJson);

        [WampProcedure("account.withdraw")]
        Task<MtClientResponse<bool>> AccountWithdraw(string requestJson);

        [WampProcedure("account.setActive")]
        Task<MtClientResponse<bool>> SetActiveAccount(string requestJson);

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

        [WampProcedure("order.positions")]
        Task<ClientPositionsLiveDemoClientResponse> GetClientOrders(string token);

        [WampProcedure("order.changeLimits")]
        Task<MtClientResponse<bool>> ChangeOrderLimits(string requestJson);

        [WampProcedure("orderbooks")]
        Task<Dictionary<string, OrderBookClientContract>> GetOrderBooks();
    }
}
