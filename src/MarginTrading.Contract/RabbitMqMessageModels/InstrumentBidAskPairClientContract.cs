// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Contract.RabbitMqMessageModels
{
    public class BidAskPairRabbitMqContract
    {
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
    }
}