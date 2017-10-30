using System;
using Common.PasswordTools;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.Common.Settings.Repositories.Azure.Entities
{
    public class ClientAccountEntity : TableEntity, IClientAccount, IPasswordKeeping
    {
        public static string GeneratePartitionKey()
        {
            return "Trader";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public DateTime Registered { get; set; }
        public string Id => RowKey;
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Pin { get; set; }
        public string NotificationsId { get; set; }
        public string Salt { get; set; }
        public string Hash { get; set; }

        public static ClientAccountEntity CreateNew(IClientAccount clientAccount, string password)
        {
            var result = new ClientAccountEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = Guid.NewGuid().ToString(),
                NotificationsId = Guid.NewGuid().ToString("N"),
                Email = clientAccount.Email.ToLower(),
                Phone = clientAccount.Phone,
                Registered = clientAccount.Registered
            };

            result.SetPassword(password);

            return result;
        }
    }
}