// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MarginTrading.OrderbookBestPricesBroker.Repositories
{
    internal interface IOrderbookBestPricesRepository
    {
        Task InsertAsync(OrderbookBestPricesHistoryEntity report, DateTime time);
    }
}