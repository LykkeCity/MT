using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.DataReader.Services
{
    public interface IOrderBookSnapshotReaderService
    {
        Task<OrderBook> GetOrderBook(string instrument);
    }
}
