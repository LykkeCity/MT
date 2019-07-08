// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories
{
    internal class AccountMarginEventsRepository : IAccountMarginEventsRepository
    {
        private readonly INoSQLTableStorage<AccountMarginEventEntity> _tableStorage;

        public AccountMarginEventsRepository(IReloadingManager<Settings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountMarginEventEntity>.Create(settings.Nested(s => s.Db.ConnString),
                "AccountMarginEvents", log);
        }

        public Task InsertOrReplaceAsync(IAccountMarginEvent entity)
        {
            return _tableStorage.InsertOrReplaceAsync(AccountMarginEventEntity.Create(entity));
        }
    }
}
