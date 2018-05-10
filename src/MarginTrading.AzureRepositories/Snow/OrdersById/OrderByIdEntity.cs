using System;
using Lykke.AzureStorage.Tables;

namespace MarginTrading.AzureRepositories.Snow.OrdersById
{
    internal class OrderByIdEntity : AzureTableEntity, IOrderById
    {
        public OrderByIdEntity()
        {
            RowKey = GenerateRowKey();
        }

        public string Id
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string AccountId { get; set; }
        public DateTime OrderCreatedTime { get; set; }

        public static string GeneratePartitionKey(string id)
        {
            return id;
        }

        public static string GenerateRowKey()
        {
            return "-";
        }

        public static OrderByIdEntity Create(IOrderById order)
        {
            return new OrderByIdEntity
            {
                Id = order.Id,
                AccountId = order.AccountId,
                OrderCreatedTime = order.OrderCreatedTime,
            };
        }
    }
}