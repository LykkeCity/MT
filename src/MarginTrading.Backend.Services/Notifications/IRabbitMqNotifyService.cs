// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Contract.RabbitMqMessageModels;

namespace MarginTrading.Backend.Services.Notifications
{
	public interface IRabbitMqNotifyService
	{
		Task OrderHistory(Order order, OrderUpdateType orderUpdateType, string activitiesMetadata = null);
		Task OrderBookPrice(InstrumentBidAskPair quote);
	    Task AccountMarginEvent(MarginEventMessage eventMessage);
		Task UpdateAccountStats(AccountStatsUpdateMessage message);
		Task NewTrade(TradeContract trade);
		Task ExternalOrder(ExecutionReport trade);
		Task PositionHistory(PositionHistoryEvent historyEvent);
	}
} 