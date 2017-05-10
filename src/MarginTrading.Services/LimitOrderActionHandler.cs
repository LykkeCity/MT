using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
    public class LimitOrderActionHandler : IEventConsumer<LimitOrderSetEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IOrderActionService _orderActionService;

        public LimitOrderActionHandler(
            IThreadSwitcher threadSwitcher,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IOrderActionService orderActionService)
        {
            _threadSwitcher = threadSwitcher;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _orderActionService = orderActionService;
        }

        public int ConsumerRank { get; } = 30;

        public void ConsumeEvent(object sender, LimitOrderSetEventArgs ea)
        {
            var deletedLimitOrders = ea.DeletedLimitOrders;

            var placedLimitOrders = ea.PlacedLimitOrders;
        }
    }
}