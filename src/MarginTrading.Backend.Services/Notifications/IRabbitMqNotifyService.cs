using System.Threading.Tasks;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.Notifications
{
	public interface IRabbitMqNotifyService
	{
		Task OrderHistory(Order order, OrderUpdateType orderUpdateType);
		Task OrderBookPrice(InstrumentBidAskPair quote);
		Task AccountUpdated(IMarginTradingAccount account);
		void Stop();
	    Task AccountMarginEvent(MarginEventMessage eventMessage);
		Task UpdateAccountStats(AccountStatsUpdateMessage message);
		Task NewTrade(TradeContract trade);
		Task ExternalOrder(ExecutionReport trade);
		Task PositionHistory(PositionHistoryEvent historyEvent);
	}
} 