using System.Threading.Tasks;
using MarginTrading.Core;

namespace MarginTrading.DataReader.Services
{
    public interface IOrderBookSnapshotReaderService
    {
        Task<OrderBook> GetOrderBook(string instrument);
    }
}
