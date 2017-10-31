using System.Threading.Tasks;
using MarginTrading.Contract.ClientContracts;
using MarginTrading.Frontend.Extensions;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/accountshistory")]
    [Authorize]
    public class AccountsHistoryController : Controller
    {
        private readonly RpcFacade _rpcFacade;

        public AccountsHistoryController(RpcFacade rpcFacade)
        {
            _rpcFacade = rpcFacade;
        }

        [Route("")]
        [HttpGet]
        public async Task<ResponseModel<AccountHistoryClientResponse>> GetAccountHistory([FromQuery]AccountHistoryFiltersClientRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<AccountHistoryClientResponse>();
            }

            var history = await _rpcFacade.GetAccountHistory(clientId, request);

            return ResponseModel<AccountHistoryClientResponse>.CreateOk(history);
        }

        [Route("timeline")]
        [HttpGet]
        public async Task<ResponseModel<AccountHistoryItemClient[]>> GetAccountHistoryTimeline([FromQuery]AccountHistoryFiltersClientRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<AccountHistoryItemClient[]>();
            }

            var history = await _rpcFacade.GetAccountHistoryTimeline(clientId, request);

            return ResponseModel<AccountHistoryItemClient[]>.CreateOk(history);
        }
    }
}
