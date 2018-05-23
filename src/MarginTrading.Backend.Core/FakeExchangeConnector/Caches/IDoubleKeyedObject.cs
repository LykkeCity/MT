namespace MarginTrading.Backend.Core.FakeExchangeConnector.Caches
{
    public interface IDoubleKeyedObject
    {
        string PartitionKey { get; }
        string RowKey { get; }
    }
}
