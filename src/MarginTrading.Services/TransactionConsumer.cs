using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
	public class TransactionConsumer : IEventConsumer<TransactionEventArgs>
	{
		private readonly IThreadSwitcher _threadSwitcher;
		private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
		private readonly IElementaryTransactionService _elementaryTransactionService;
		private readonly IEventChannel<ElementaryTransactionEventArgs> _elementaryTransactionEventChannel;

		public TransactionConsumer(IThreadSwitcher threadSwitcher,
			IRabbitMqNotifyService rabbitMqNotifyService,
			IElementaryTransactionService elementaryTransactionService,
			IEventChannel<ElementaryTransactionEventArgs> elementaryTransactionEventChannel)
		{
			_threadSwitcher = threadSwitcher;
			_rabbitMqNotifyService = rabbitMqNotifyService;
			_elementaryTransactionService = elementaryTransactionService;
			_elementaryTransactionEventChannel = elementaryTransactionEventChannel;
		}

		int IEventConsumer.ConsumerRank => 100;

		void IEventConsumer<TransactionEventArgs>.ConsumeEvent(object sender, TransactionEventArgs ea)
		{
			var transaction = ea.Transaction;
			_threadSwitcher.SwitchThread(async () =>
			{
				await _elementaryTransactionService.CreateElementaryTransactionsAsync(transaction, async elementaryTransaction =>
				{
					_elementaryTransactionEventChannel.SendEvent(this, new ElementaryTransactionEventArgs(elementaryTransaction));

					await _rabbitMqNotifyService.ElementaryTransactionCreated(elementaryTransaction);
				});
			});
		}
	}
}