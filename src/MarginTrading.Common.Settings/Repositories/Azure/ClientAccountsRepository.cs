using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;

namespace MarginTrading.Common.Settings.Repositories.Azure
{
    //TODO: use http://client-account.lykke-service.svc.cluster.local/swagger/ui/index.html
    public class ClientsRepository : IClientAccountsRepository
    {
        private readonly INoSQLTableStorage<ClientAccountEntity> _clientsTablestorage;
        private readonly INoSQLTableStorage<AzureIndex> _emailIndices;

        private const string IndexEmail = "IndexEmail";

        public ClientsRepository(INoSQLTableStorage<ClientAccountEntity> clientsTablestorage, INoSQLTableStorage<AzureIndex> emailIndices)
        {
            _clientsTablestorage = clientsTablestorage;
            _emailIndices = emailIndices;
        }

        public async Task<IClientAccount> GetByIdAsync(string id)
        {
            var partitionKey = ClientAccountEntity.GeneratePartitionKey();
            var rowKey = ClientAccountEntity.GenerateRowKey(id);

            return await _clientsTablestorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<IClientAccount> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return await _clientsTablestorage.GetDataAsync(_emailIndices, IndexEmail, email.ToLower());
        }
    }
}
