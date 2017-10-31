namespace MarginTrading.Contract.ClientContracts
{
    public class CloseOrderRpcClientRequest : CloseOrderClientRequest
    {
        public string Token { get; set; }
    }
}