using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Common.ClientContracts;
using MarginTrading.Frontend.Extensions;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/init")]
    [Authorize]
    public class InitController : Controller
    {
        private readonly RpcFacade _rpcFacade;

        public InitController(RpcFacade rpcFacade)
        {
            _rpcFacade = rpcFacade;
        }

        [Route("data")]
        [HttpGet]
        public async Task<ResponseModel<InitDataLiveDemoClientResponse>> InitData()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<InitDataLiveDemoClientResponse>();
            }

            var initData = await _rpcFacade.InitData(clientId);

            return ResponseModel<InitDataLiveDemoClientResponse>.CreateOk(initData);
        }

        [Route("accounts")]
        [HttpGet]
        public async Task<ResponseModel<InitAccountsLiveDemoClientResponse>> InitAccounts()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<InitAccountsLiveDemoClientResponse>();
            }

            var initAccounts = await _rpcFacade.InitAccounts(clientId);

            return ResponseModel<InitAccountsLiveDemoClientResponse>.CreateOk(initAccounts);
        }

        [Route("accountinstruments")]
        [HttpGet]
        public async Task<ResponseModel<InitAccountInstrumentsLiveDemoClientResponse>> AccountInstruments()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<InitAccountInstrumentsLiveDemoClientResponse>();
            }

            var initAccountInstruments = await _rpcFacade.AccountInstruments(clientId);

            return ResponseModel<InitAccountInstrumentsLiveDemoClientResponse>.CreateOk(initAccountInstruments);
        }

        [Route("graph")]
        [HttpGet]
        public async Task<ResponseModel<InitChartDataClientResponse>> InitGraph()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<InitChartDataClientResponse>();
            }

            var initGraph = await _rpcFacade.InitGraph(clientId);

            return ResponseModel<InitChartDataClientResponse>.CreateOk(initGraph);
        }

        [Route("chart/filtered")]
        [HttpPost]
        public async Task<ResponseModel<InitChartDataClientResponse>> InitChartFiltered([FromBody] InitChartDataClientRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<InitChartDataClientResponse>();
            }


            var initGraph = await _rpcFacade.InitGraph(clientId, request?.AssetIds);

            return ResponseModel<InitChartDataClientResponse>.CreateOk(initGraph);
        }

        [Route("prices")]
        [HttpGet]
        public async Task<ResponseModel<Dictionary<string, BidAskClientContract>>> InitPrices()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<Dictionary<string, BidAskClientContract>>();
            }

            var initPrices = await _rpcFacade.InitPrices(clientId);

            return ResponseModel<Dictionary<string, BidAskClientContract>>.CreateOk(initPrices);
        }

        [Route("prices/filtered")]
        [HttpPost]
        public async Task<ResponseModel<Dictionary<string, BidAskClientContract>>> InitPricesWithFilter([FromBody] InitPricesFilteredRequest request)
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<Dictionary<string, BidAskClientContract>>();
            }

            var initPrices = await _rpcFacade.InitPrices(clientId, request?.AssetIds);

            return ResponseModel<Dictionary<string, BidAskClientContract>>.CreateOk(initPrices);
        }
    }
}
