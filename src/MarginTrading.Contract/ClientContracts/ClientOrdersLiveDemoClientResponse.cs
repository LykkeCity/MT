namespace MarginTrading.Contract.ClientContracts
{
    public class ClientOrdersLiveDemoClientResponse
    {
        public OrderClientContract[] Live { get; set; }
        public OrderClientContract[] Demo { get; set; }
    }
}