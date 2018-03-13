using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.ExternalOrderBroker.Models;
using MarginTrading.ExternalOrderBroker.Repositories;

namespace MarginTrading.ExternalOrderBroker
{
    public class Application : BrokerApplicationBase<Lykke.Service.ExchangeConnector.Client.Models.ExecutionReport>
    {
        private readonly IExternalOrderReportRepository _externalOrderReportRepository;
        private readonly Settings.AppSettings _appSettings;

        public Application(IExternalOrderReportRepository externalOrderReportRepository,
            ILog logger,
            Settings.AppSettings appSettings, 
            CurrentApplicationInfo applicationInfo,
            ISlackNotificationsSender slackNotificationsSender) 
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _externalOrderReportRepository = externalOrderReportRepository;
            _appSettings = appSettings;
        }

        protected override BrokerSettingsBase Settings => _appSettings;
        protected override string ExchangeName => _appSettings.RabbitMqQueues.ExternalOrder.ExchangeName;

        protected override Task HandleMessage(Lykke.Service.ExchangeConnector.Client.Models.ExecutionReport order)
        {
            var externalOrder = ExternalOrderReport.Create(order);
            return _externalOrderReportRepository.InsertOrReplaceAsync(externalOrder);
        }
    }
}