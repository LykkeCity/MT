using System;
using System.Threading.Tasks;
using MarginTrading.Core.RabbitMqMessages;

namespace MarginTrading.Core
{
	public interface IRabbitMqNotifyService
	{
		Task AccountHistory(string accountId, string clientId, decimal amount, decimal balance, decimal withdrawTransferLimit, AccountHistoryType type, string comment = null);
		Task OrderHistory(IOrder order);
		Task OrderReject(IOrder order);
		Task OrderBookPrice(InstrumentBidAskPair quote);
		Task OrderChanged(IOrder order);
		Task AccountUpdated(IMarginTradingAccount account);
		Task AccountStopout(string clientId, string accountId, int positionsCount, decimal totalPnl);
		Task UserUpdates(bool updateAccountAssets, bool updateAccounts, string[] clientIds);
		void Stop();
	    Task AccountCreated(IMarginTradingAccount account);
	    Task AccountDeleted(IMarginTradingAccount account);
	    Task AccountMarginEvent(AccountMarginEventMessage eventMessage);
	}
}