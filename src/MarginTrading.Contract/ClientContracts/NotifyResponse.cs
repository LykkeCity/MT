namespace MarginTrading.Contract.ClientContracts
{
    public class NotifyResponse<T>
    {
        public NotifyEntityType Type { get; set; }
        public T Entity { get; set; }
    }

    public class NotifyResponse
    {
        public MarginTradingAccountClientContract Account { get; set; }
        public OrderClientContract Order { get; set; }
        public AccountStopoutClientContract AccountStopout{ get; set; }
        public UserUpdateEntityClientContract UserUpdate { get; set; }
    }
}
