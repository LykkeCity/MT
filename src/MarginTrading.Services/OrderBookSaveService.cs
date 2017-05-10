using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class OrderBookSaveService : TimerPeriod
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly OrderBookList _orderBookList;

        public OrderBookSaveService(
            IMarginTradingBlobRepository blobRepository,
            OrderBookList orderBookList,
            ILog log
            ) : base(nameof(OrderBookSaveService), 1000, log)
        {
            _blobRepository = blobRepository;
            _orderBookList = orderBookList;
        }

        public override async Task Execute()
        {
            // TOOD: Implement orderbook save and restore
            //throw new NotImplementedException();

            //try
            //{
            //    var orderbookState = _orderBookList.GetOrderBookState();

            //    if (orderbookState != null)
            //    {
            //        await _blobRepository.Write("margintrading", "orderbook", orderbookState);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

        }
    }
}
