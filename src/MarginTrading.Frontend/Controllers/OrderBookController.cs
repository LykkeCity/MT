using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/orderbook")]
    [Authorize]
    public class OrderBookController : Controller
    {
        private readonly RpcFacade _rpcFacade;

        public OrderBookController(RpcFacade rpcFacade)
        {
            _rpcFacade = rpcFacade;
        }

        [HttpGet]
        [Route("{instrument}")]
        public async Task<ResponseModel<OrderBookClientContract>> GetOrderBook(string instrument)
        {
            var result = await _rpcFacade.GetOrderBook(instrument);

            return ResponseModel<OrderBookClientContract>.CreateOk(result);
        }
    }
}
