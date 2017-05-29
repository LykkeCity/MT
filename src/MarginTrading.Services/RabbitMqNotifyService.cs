using System;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Common;
using Common.Log;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
	public class RabbitMqNotifyService : IRabbitMqNotifyService
	{
		private readonly MarginSettings _settings;
		private readonly IIndex<string, IMessageProducer<string>> _publishers;
		private readonly ILog _log;

		public RabbitMqNotifyService(
			MarginSettings settings,
			IIndex<string, IMessageProducer<string>> publishers,
			ILog log)
		{
			_settings = settings;
			_publishers = publishers;
			_log = log;
		}
		public async Task AccountHistory(string accountId, string clientId, double amount, double balance, AccountHistoryType type, string comment = null)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.AccountHistory.ExchangeName].ProduceAsync(new MarginTradingAccountHistory
				{
					Id = Guid.NewGuid().ToString("N"),
					AccountId = accountId,
					ClientId = clientId,
					Type = type,
					Amount = amount,
					Balance = balance,
					Date = DateTime.UtcNow,
					Comment = comment
				}.ToBackendContract().ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(AccountHistory),
					$"accountId: {accountId}, clientId: {clientId}, amount: {amount}, balance: {balance}, type: {type}, comment: {comment}",
					ex);
			}
		}

		public async Task OrderHistory(IOrder order)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.OrderHistory.ExchangeName].ProduceAsync(order.ToFullContract().ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(OrderHistory), $"orderId: {order.Id}, accountId: {order.AccountId}, clientId: {order.ClientId}",
					ex);
			}
		}

		public async Task OrdeReject(IOrder order)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.OrderRejected.ExchangeName].ProduceAsync(order.ToFullContract().ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(OrdeReject),
					$"orderId: {order.Id}, accountId: {order.AccountId}, clientId: {order.ClientId}",
					ex);
			}
		}

		public async Task OrderBookPrice(InstrumentBidAskPair quote)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.OrderbookPrices.ExchangeName].ProduceAsync(quote.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(OrderBookPrice),
					$"instrument: {quote.Instrument}, bid: {quote.Bid}, ask: {quote.Ask}, data: {quote.Date:u}",
					ex);
			}
		}

		public async Task OrderChanged(IOrder order)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.OrderChanged.ExchangeName].ProduceAsync(order.ToBaseContract().ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(OrderChanged),
					$"orderId: {order.Id}, accountId: {order.AccountId}, clientId: {order.ClientId}",
					ex);
			}
		}

		public async Task AccountChanged(IMarginTradingAccount account)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.AccountChanged.ExchangeName].ProduceAsync(account.ToBackendContract(_settings.IsLive).ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(AccountChanged),
					$"accountId: {account.Id}, clientId: {account.ClientId}",
					ex);
			}
		}

		public async Task AccountStopout(string clientId, string accountId, int positionsCount, double totalPnl)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.AccountStopout.ExchangeName].ProduceAsync(new { clientId = clientId, accountId = accountId, positionsCount = positionsCount, totalPnl = totalPnl }.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(AccountStopout),
					$"accountId: {accountId}, positions count: {positionsCount}, total PnL: {totalPnl}", ex);
			}
		}

		public async Task UserUpdates(bool updateAccountAssets, bool updateAccounts, string[] clientIds)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.UserUpdates.ExchangeName].ProduceAsync(new { updateAccountAssetPairs = updateAccountAssets, UpdateAccounts = updateAccounts, clientIds = clientIds }.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(UserUpdates), null, ex);
			}
		}

		public async Task TransactionCreated(ITransaction transaction)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.Transaction.ExchangeName].ProduceAsync(transaction.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(TransactionCreated), $"takerOrderId: {transaction.TakerPositionId}, takerAccountId: {transaction.TakerAccountId}, takerId: {transaction.TakerCounterpartyId}, makerOrderId: {transaction.MakerOrderId}, makerId: {transaction.MakerCounterpartyId}", ex);
			}
		}

		public async Task PositionUpdated(IPosition position)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.PositionUpdates.ExchangeName].ProduceAsync(position.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(PositionUpdated), $"counterPartyId: {position.ClientId}, assetId: {position.Asset}", ex);
			}
		}

		public async Task ElementaryTransactionCreated(IElementaryTransaction transaction)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.ElementaryTransaction.ExchangeName].ProduceAsync(transaction.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(ElementaryTransactionCreated), $"transactionId: {transaction.TradingTransactionId}, counterParty: {transaction.CounterPartyId}, asset: {transaction.Asset}, type: {transaction.SubType}", ex);
			}
		}

		public async Task TradingOrderCreated(ITradingOrder orderAction)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.OrderReport.ExchangeName].ProduceAsync(orderAction.ToJson());
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(TradingOrderCreated), $"orderId: {orderAction.TakerPositionId}, traderId: {orderAction.TakerCounterpartyId}", ex);
			}
		}

		public async Task HardTradingLimitReached(string counterPartyId)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.ValueAtRiskLimits.ExchangeName].ProduceAsync(counterPartyId);
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(HardTradingLimitReached), $"counterPartyId: {counterPartyId}", ex);
			}
		}

		public async Task IndividualValueAtRiskSet(string counterPartyId, string assetId, double value)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.IndividualValuesAtRisk.ExchangeName].ProduceAsync($"{counterPartyId};{assetId};{value}");
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(IndividualValueAtRiskSet), $"counterPartyId: {counterPartyId}, assetId: {assetId}", ex);
			}
		}

		public async Task AggregateValueAtRiskSet(string counterPartyId, double value)
		{
			try
			{
				await _publishers[_settings.RabbitMqQueues.AggregateValuesAtRisk.ExchangeName].ProduceAsync($"{counterPartyId};{value}");
			}
			catch (Exception ex)
			{
				await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(AggregateValueAtRiskSet), $"counterPartyId: {counterPartyId}", ex);
			}
		}

		public void Stop()
		{
			((IStopable)_publishers[_settings.RabbitMqQueues.AccountHistory.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.OrderHistory.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.OrderRejected.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.OrderbookPrices.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.AccountStopout.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.AccountChanged.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.UserUpdates.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.Transaction.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.ElementaryTransaction.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.OrderReport.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.ValueAtRiskLimits.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.IndividualValuesAtRisk.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.AggregateValuesAtRisk.ExchangeName]).Stop();
			((IStopable)_publishers[_settings.RabbitMqQueues.PositionUpdates.ExchangeName]).Stop();
		}
	}
}
