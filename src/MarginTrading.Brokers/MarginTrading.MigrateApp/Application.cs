using System;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.SlackNotifications;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.BrokerBase;
using MarginTrading.BrokerBase.Settings;
using MarginTrading.Contract.RabbitMqMessageModels;
using MoreLinq;

namespace MarginTrading.MigrateApp
{
    internal class Application : BrokerApplicationBase<BidAskPairRabbitMqContract>
    {
        private readonly Settings _settings;
        private readonly IReloadingManager<Settings> _reloadingManager;

        public Application(ILog logger, Settings settings,
            CurrentApplicationInfo applicationInfo,
            ISlackNotificationsSender slackNotificationsSender,
            IReloadingManager<Settings> reloadingManager)
            : base(logger, slackNotificationsSender, applicationInfo)
        {
            _settings = settings;
            _reloadingManager = reloadingManager;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => "Fake";

        protected override Task HandleMessage(BidAskPairRabbitMqContract message)
        {
            throw new NotSupportedException();
        }

        public override void Run()
        {
            WriteInfoToLogAndSlack("Starting MigrateApp");

            try
            {
                Task.WaitAll(
                    Task.Run(ProcessOrders)
                );
                WriteInfoToLogAndSlack("MigrateApp finished");
            }
            catch (Exception ex)
            {
                _logger.WriteErrorAsync(ApplicationInfo.ApplicationFullName, "Application.RunAsync", null, ex)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private async Task ProcessOrders()
        {
            var repository = AzureTableStorage<MarginTradingOrderHistoryEntity>.Create(
                _reloadingManager.Nested(s => s.Db.MarginTradingConnString), "MarginTradingOrdersHistory", _logger);
            var tasks = (await repository.GetDataAsync())
                .Where(a => a.OrderUpdateType == null && a.Status != "Closed")
                .GroupBy(a => a.PartitionKey)
                .SelectMany(g => g.Batch(500))
                .Select(batch => repository.DeleteAsync(batch));
            await Task.WhenAll(tasks);
        }
    }
}