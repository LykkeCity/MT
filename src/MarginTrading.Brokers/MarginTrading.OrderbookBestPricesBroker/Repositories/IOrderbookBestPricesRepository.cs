using System;
using System.Threading.Tasks;

namespace MarginTrading.OrderbookBestPricesBroker.Repositories
{
    internal interface IOrderbookBestPricesRepository
    {
        Task InsertOrReplaceAsync(OrderbookBestPricesEntity report, DateTime time);
    }
}