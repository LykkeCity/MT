namespace MarginTrading.Contract.RabbitMqMessageModels
{
    public class AccountStatsUpdateMessage
    {
        public AccountStatsContract[] Accounts { get; set; }
    }
}