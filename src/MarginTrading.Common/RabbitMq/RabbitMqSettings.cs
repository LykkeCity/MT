﻿namespace MarginTrading.Common.RabbitMq
{
    public class RabbitMqSettings
    {
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        public bool IsDurable { get; set; }
    }
}