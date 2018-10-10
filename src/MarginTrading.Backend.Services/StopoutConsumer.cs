using Common;
using Lykke.Common;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    // TODO: Rename by role
    public class StopOutConsumer : IEventConsumer<StopOutEventArgs>
    {
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IOperationsLogService _operationsLogService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;

        public StopOutConsumer(IThreadSwitcher threadSwitcher,
            IOperationsLogService operationsLogService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService)
        {
            _threadSwitcher = threadSwitcher;
            _operationsLogService = operationsLogService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<StopOutEventArgs>.ConsumeEvent(object sender, StopOutEventArgs ea)
        {
            var account = ea.Account;
            var eventTime = _dateService.Now();
            var accountMarginEventMessage =
                AccountMarginEventMessageConverter.Create(account, MarginEventTypeContract.Stopout, eventTime);

            _threadSwitcher.SwitchThread(async () =>
            {
                _operationsLogService.AddLog("stopout", account.Id, "", ea.ToJson());

                await _rabbitMqNotifyService.AccountMarginEvent(accountMarginEventMessage);
            });
        }
    }
}