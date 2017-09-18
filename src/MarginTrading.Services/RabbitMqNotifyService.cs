using System;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Common;
using Common.Log;
using MarginTrading.Common.Mappers;
using MarginTrading.Common.RabbitMqMessages;
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
		public async Task AccountHistory(string accountId, string clientId, double amount, double balance, double withdrawTransferLimit, AccountHistoryType type, string comment = null)
		{
		    var record = new MarginTradingAccountHistory
		    {
		        Id = Guid.NewGuid().ToString("N"),
		        AccountId = accountId,
		        ClientId = clientId,
		        Type = type,
		        Amount = amount,
		        Balance = balance,
		        WithdrawTransferLimit = withdrawTransferLimit,
		        Date = DateTime.UtcNow,
		        Comment = comment
		    };

            try
			{
				await _publishers[_settings.RabbitMqQueues.AccountHistory.ExchangeName].ProduceAsync(record.ToBackendContract().ToJson());
			}
			catch (Exception ex)
			{
			    await _log.WriteErrorAsync(nameof(RabbitMqNotifyService), nameof(AccountHistory), record.ToJson(), ex);
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

	    public Task AccountMarginEvent(IMarginTradingAccount account, bool isStopout, DateTime eventTime)
	    {
	        return TryProduceMessageAsync(_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName,
	            AccountMarginEventMessage.Create(account, isStopout, eventTime));

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

	    private async Task TryProduceMessageAsync(string exchangeName, object message)
	    {
	        string messageStr = null;
	        try
	        {
	            messageStr = message.ToJson();
	            await _publishers[exchangeName].ProduceAsync(messageStr);
	        }
	        catch (Exception ex)
	        {
#pragma warning disable 4014
	            _log.WriteErrorAsync(nameof(RabbitMqNotifyService), exchangeName, messageStr, ex);
#pragma warning restore 4014
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
			((IStopable)_publishers[_settings.RabbitMqQueues.AccountMarginEvents.ExchangeName]).Stop();
		}
	}
}
