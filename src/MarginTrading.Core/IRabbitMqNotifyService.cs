using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IRabbitMqNotifyService
	{
		Task AccountHistory(string accountId, string clientId, double amount, double balance, AccountHistoryType type, string comment = null);
		Task OrderHistory(IOrder order);
		Task OrdeReject(IOrder order);
		Task OrderBookPrice(InstrumentBidAskPair quote);
		Task OrderChanged(IOrder order);
		Task AccountChanged(IMarginTradingAccount account);
		Task AccountStopout(string clientId, string accountId, int positionsCount, double totalPnl);
		Task UserUpdates(bool updateAccountAssets, bool updateAccounts, string[] clientIds);
		Task TransactionCreated(ITransaction transaction);
		Task ElementaryTransactionCreated(IElementaryTransaction elementaryTransaction);
		Task TradingOrderCreated(ITradingOrder order);
		Task HardTradingLimitReached(string counterPartyId);
		Task PositionUpdated(IPosition position);
		Task IndividualValueAtRiskSet(string counterPartyId, string assetId, double value);
		Task AggregateValueAtRiskSet(string counterPartyId, double value);
		void Stop();
	}
}