namespace MarginTrading.Common.ClientContracts
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

    public class AccountStopoutBackendContract
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public int PositionsCount { get; set; }
        public decimal TotalPnl { get; set; }
    }

    public class AccountStopoutClientContract
    {
        public string AccountId { get; set; }
        public int PositionsCount { get; set; }
        public decimal TotalPnl { get; set; }
    }

    public class UserUpdateEntityBackendContract
    {
        public string[] ClientIds { get; set; }
        public bool UpdateAccountAssetPairs { get; set; }
        public bool UpdateAccounts { get; set; }
    }

    public class UserUpdateEntityClientContract
    {
        public bool UpdateAccountAssetPairs { get; set; }
        public bool UpdateAccounts { get; set; }
    }

    public enum NotifyEntityType
    {
        Account,
        Order,
        AccountStopout,
        UserUpdate
    }
}
