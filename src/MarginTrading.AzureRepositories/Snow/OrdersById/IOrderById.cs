using System;

namespace MarginTrading.AzureRepositories.Snow.OrdersById
{
    public interface IOrderById
    {
        string Id { get; }
        string AccountId { get; }
        DateTime OrderCreatedTime { get; }
    }
}