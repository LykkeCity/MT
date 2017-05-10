namespace MarginTrading.Common.ClientContracts
{
    public class ClientPositionsLiveDemoClientResponse
    {
        public ClientOrdersClientResponse Live { get; set; }
        public ClientOrdersClientResponse Demo { get; set; }
    }

    public class ClientOrdersClientResponse
    {
        public OrderClientContract[] Positions { get; set; }
        public OrderClientContract[] Orders { get; set; }
    }
}
