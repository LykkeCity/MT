// Copyright (c) 2019 Lykke Corp.

using System;
using System.Threading.Tasks;

namespace MarginTrading.OrderbookBestPricesBroker.Repositories
{
    internal interface IOrderbookBestPricesRepository
    {
        Task InsertAsync(OrderbookBestPricesHistoryEntity report, DateTime time);
    }
}