using System;

namespace MarginTrading.AzureRepositories.Snow.OrdersById
{
    public class OrderById : IOrderById
    {
        public OrderById(string id, string accountId, DateTime orderCreatedTime)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            OrderCreatedTime = orderCreatedTime;
        }

        public string Id { get; }
        public string AccountId { get; }
        public DateTime OrderCreatedTime { get; }
    }
}