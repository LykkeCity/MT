﻿using Lykke.AzureStorage.Tables;

namespace MarginTrading.OrderbookBestPricesBroker.Repositories
{
    internal class OrderbookBestPricesHistoryEntity : AzureTableEntity
    {
        public string AssetPairId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
    }
}