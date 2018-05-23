namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain
{
    public interface ISignedModel
    {
        string GetStringToSign();
    }
}