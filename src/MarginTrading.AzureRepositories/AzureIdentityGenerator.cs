using System;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.AzureRepositories
{
    public class AzureIdentityGenerator : IIdentityGenerator
    {
        private readonly INoSQLTableStorage<IdentityEntity> _tableStorage;

        public AzureIdentityGenerator(INoSQLTableStorage<IdentityEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<long> GenerateIdAsync(string entityType)
        {
            long id = 0;
            var result =
                await
                    _tableStorage.InsertOrModifyAsync(IdentityEntity.GeneratePartitionKey(entityType),
                        IdentityEntity.GenerateRowKey,
                        () => IdentityEntity.Create(entityType),
                        itm =>
                        {
                            itm.Value++;
                            id = itm.Value;
                            return true;
                        }
                    );


            if (!result)
                throw new InvalidOperationException("Error generating ID");

            return id;
        }

        public string GenerateAlphanumericId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string GenerateGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}