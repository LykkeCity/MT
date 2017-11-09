using Common.Log;
using Flurl.Http;
using MarginTrading.AccountHistoryBroker.Repositories.SqlRepositories;
using MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories;
using MarginTrading.AccountReportsBroker.Repositories.SqlRepositories;
using MarginTrading.SqlMigration.Repositories.Azure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.SqlMigration
{
    class MigrationService
    {
        private readonly MigrationSettings _settings;
        private readonly ILog _log;

        public MigrationService(ILog log)
        {
            _log = log;

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.dev.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            // If it's Development environment load local settings file, else load settings from Lykke.Settings
            _settings = (env == "Development") ?
                    config.Get<MigrationSettings>() :
                    Lykke.SettingsReader.SettingsProcessor.Process<MigrationSettings>(config["SettingsUrl"].GetStringAsync().Result);
        }

        public async Task<long> Migrate_MarginTradingAccountTransactionsReports()
        {
            var brokerSettings = new AccountHistoryBroker.Settings
            {
                Db = new AccountHistoryBroker.Db
                {
                    ReportsSqlConnString = _settings.SqlConnString
                }
            };

            AccountTransactionsReportsRepository AzureRepo = new AccountTransactionsReportsRepository(_settings.AzureConnString, _log);
            AccountTransactionsReportsSqlRepository SqlRepo = new AccountTransactionsReportsSqlRepository(brokerSettings, _log);

            var records = await AzureRepo.GetData(DateTime.MinValue, DateTime.UtcNow.Date);
            long res = 0;
            foreach (var item in records)
            {
                await SqlRepo.InsertOrReplaceAsync(item);
                res++;
            }
            return res;
        }

        public async Task<long> Migrate_AccountMarginEventsReports()
        {
            var brokerSettings = new AccountMarginEventsBroker.Settings
            {
                Db = new AccountMarginEventsBroker.Db
                {
                    ReportsSqlConnString = _settings.SqlConnString
                }
            };

            AccountMarginEventsReportsRepository AzureRepo = new AccountMarginEventsReportsRepository(_settings.AzureConnString, _log);
            AccountMarginEventsReportsSqlRepository SqlRepo = new AccountMarginEventsReportsSqlRepository(brokerSettings, _log);

            var records = await AzureRepo.GetData(DateTime.MinValue, DateTime.UtcNow.Date);
            long res = 0;
            foreach (var item in records)
            {
                await SqlRepo.InsertOrReplaceAsync(item);
                res++;
            }
            return res;
        }

        public async Task<long> Migrate_ClientAccountsReports()
        {
            var brokerSettings = new AccountReportsBroker.Settings
            {
                Db = new AccountReportsBroker.Db
                {
                    ReportsSqlConnString = _settings.SqlConnString
                }
            };

            AccountsReportsRepository AzureRepo = new AccountsReportsRepository(_settings.AzureConnString, _log);
            AccountsReportsSqlRepository SqlRepo = new AccountsReportsSqlRepository(brokerSettings, _log);

            var records = await AzureRepo.GetData(DateTime.MinValue, DateTime.UtcNow.Date);
            long res = 0;
            foreach (var item in records)
            {
                await SqlRepo.InsertOrReplaceAsync(item);
                res++;
            }
            return res;
        }

        public async Task<long> Migrate_ClientAccountsStatusReports()
        {
            var brokerSettings = new AccountReportsBroker.Settings
            {
                Db = new AccountReportsBroker.Db
                {
                    ReportsSqlConnString = _settings.SqlConnString
                }
            };

            AccountsStatsReportsRepository AzureRepo = new AccountsStatsReportsRepository(_settings.AzureConnString, _log);
            AccountsStatsReportsSqlRepository SqlRepo = new AccountsStatsReportsSqlRepository(brokerSettings, _log);

            var records = await AzureRepo.GetData(DateTime.MinValue, DateTime.UtcNow.Date);            
            await SqlRepo.InsertOrReplaceBatchAsync(records);
            return records.Count();
        }


    }
}
