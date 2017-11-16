﻿using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.Notifications
{
	public interface IRabbitMqNotifyService
	{
		Task AccountHistory(string accountId, string clientId, decimal amount, decimal balance, decimal withdrawTransferLimit, AccountHistoryType type, string comment = null, string eventSourceId = null);
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
		Task UpdateAccountStats(AccountStatsUpdateMessage message);
		Task NewTrade(TradeContract trade);
	}
} 