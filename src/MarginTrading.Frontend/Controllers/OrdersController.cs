using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Frontend.Extensions;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Services;
using MarginTrading.Services.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/orders")]
    [Authorize]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class OrdersController : Controller
    {
        private readonly RpcFacade _rpcFacade;

        public OrdersController(RpcFacade rpcFacade)
        {
            _rpcFacade = rpcFacade;
        }

        [Route("place")]
        [HttpPost]
        public async Task<ResponseModel<OrderClientContract>> PlaceOrder([FromBody] NewOrderClientContract request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<OrderClientContract>();
            }

            var result = await _rpcFacade.PlaceOrder(clientId, request);

            if (result.IsError())
            {
                return ResponseModel<OrderClientContract>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData,
                    result.Message);
            }

            return ResponseModel<OrderClientContract>.CreateOk(result.Result);
        }

        [Route("close")]
        [HttpPost]
        public async Task<ResponseModel<bool>> CloseOrder([FromBody] CloseOrderClientRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<bool>();
            }

            var result = await _rpcFacade.CloseOrder(clientId, request);

            if (result.IsError() || !result.Result)
            {
                return ResponseModel<bool>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData,
                    result.Message);
            }

            return ResponseModel<bool>.CreateOk(result.Result);
        }

        [Route("cancel")]
        [HttpPost]
        public async Task<ResponseModel<bool>> CancelOrder([FromBody] CloseOrderClientRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<bool>();
            }

            var result = await _rpcFacade.CancelOrder(clientId, request);

            if (result.IsError() || !result.Result)
            {
                return ResponseModel<bool>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData,
                    result.Message);
            }

            return ResponseModel<bool>.CreateOk(result.Result);
        }

        [Route("positions")]
        [HttpGet]
        public async Task<ResponseModel<ClientOrdersLiveDemoClientResponse>> GetOpenPositions()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<ClientOrdersLiveDemoClientResponse>();
            }

            var result = await _rpcFacade.GetOpenPositions(clientId);

            return ResponseModel<ClientOrdersLiveDemoClientResponse>.CreateOk(result);
        }

        [Route("positions/{accountId}")]
        [HttpGet]
        public async Task<ResponseModel<OrderClientContract[]>> GetAccountOpenPositions(string accountId)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<OrderClientContract[]>();
            }

            var result = await _rpcFacade.GetAccountOpenPositions(clientId, accountId);

            return ResponseModel<OrderClientContract[]>.CreateOk(result);
        }

        [Route("")]
        [HttpGet]
        public async Task<ResponseModel<ClientPositionsLiveDemoClientResponse>> GetClientOrders()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<ClientPositionsLiveDemoClientResponse>();
            }

            var result = await _rpcFacade.GetClientOrders(clientId);

            return ResponseModel<ClientPositionsLiveDemoClientResponse>.CreateOk(result);
        }

        [Route("limits")]
        [HttpPut]
        public async Task<ResponseModel<bool>> ChangeOrderLimits([FromBody] ChangeOrderLimitsClientRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<bool>();
            }

            var result = await _rpcFacade.ChangeOrderLimits(clientId, request);

            if (result.IsError() || !result.Result)
            {
                return ResponseModel<bool>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData,
                    result.Message);
            }

            return ResponseModel<bool>.CreateOk(result.Result);
        }
    }
}
