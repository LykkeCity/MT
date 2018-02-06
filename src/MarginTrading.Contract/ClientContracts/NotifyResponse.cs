using System.ComponentModel;

namespace MarginTrading.Contract.ClientContracts
{
    public class NotifyResponse<T>
    {
        public NotifyEntityType Type { get; set; }
        public T Entity { get; set; }
    }

    [DisplayName("User update message")]
    public class NotifyResponse
    {
        [DisplayName("Updated account info, if exists")]
        public MarginTradingAccountClientContract Account { get; set; }
        [DisplayName("Updated order info, if exists")]
        public OrderClientContract Order { get; set; }
        [DisplayName("Stopout info, if exists")]
        public AccountStopoutClientContract AccountStopout { get; set; }
        [DisplayName("Updated user info, if exists")]
        public UserUpdateEntityClientContract UserUpdate { get; set; }
    }
}
