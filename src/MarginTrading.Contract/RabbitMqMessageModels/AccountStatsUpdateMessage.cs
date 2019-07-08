// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.RabbitMqMessageModels
{
    public class AccountStatsUpdateMessage
    {
        public AccountStatsContract[] Accounts { get; set; }
    }
}