using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarginTrading.DataReader.Services;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountHistory")]
    public class AccountHistoryController : Controller
    {
        private readonly IMarginTradingAccountHistoryRepository _accountsHistoryRepository;
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IOrdersSnapshotReaderService _ordersSnapshotReaderService;
        private readonly IMarginTradingAccountsRepository _accountsRepository;

        public AccountHistoryController(
            IMarginTradingAccountsRepository accountsRepository,
            IMarginTradingAccountHistoryRepository accountsHistoryRepository,
            IMarginTradingOrdersHistoryRepository ordersHistoryRepository,
            IOrdersSnapshotReaderService ordersSnapshotReaderService)
        {
            _accountsRepository = accountsRepository;
            _accountsHistoryRepository = accountsHistoryRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _ordersSnapshotReaderService = ordersSnapshotReaderService;
        }

        [Route("byTypes")]
        [HttpGet]
        public async Task<AccountHistoryBackendResponse> GetByTypes([FromQuery]AccountHistoryBackendRequest request)
        {
            var clientAccountIds = string.IsNullOrEmpty(request.AccountId)
                    ? (await _accountsRepository.GetAllAsync(request.ClientId)).Select(item => item.Id).ToArray()
                    : new[] { request.AccountId };

            var accounts = (await _accountsHistoryRepository.GetAsync(clientAccountIds, request.From, request.To))
                .Where(item => item.Type != AccountHistoryType.OrderClosed);

            var orders = (await _ordersHistoryRepository.GetHistoryAsync(request.ClientId, clientAccountIds, request.From, request.To))
                .Where(item => item.Status != OrderStatus.Rejected);

            var openPositions = await _ordersSnapshotReaderService.GetActiveByAccountIdsAsync(clientAccountIds);

            return AccountHistoryBackendResponse.Create(accounts, openPositions, orders);
        }

        [Route("byAccounts")]
        [HttpGet]
        public async Task<Dictionary<string, AccountHistoryBackendContract[]>> GetByAccounts(
            [FromQuery] string accountId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var accountIds = accountId != null
                ? new[] { accountId }
                : (await _accountsRepository.GetAllAsync()).Select(item => item.Id).ToArray();

            return (await _accountsHistoryRepository.GetAsync(accountIds, from, to)).GroupBy(i => i.AccountId)
                .ToDictionary(g => g.Key, g => g.Select(i => i.ToBackendContract()).ToArray());
        }

        [Route("timeline")]
        [HttpGet]
        public async Task<AccountNewHistoryBackendResponse> GetTimeline([FromQuery]AccountHistoryBackendRequest request)
        {
            var clientAccountIds = string.IsNullOrEmpty(request.AccountId)
                    ? (await _accountsRepository.GetAllAsync(request.ClientId)).Select(item => item.Id).ToArray()
                    : new[] { request.AccountId };

            var accounts = (await _accountsHistoryRepository.GetAsync(clientAccountIds, request.From, request.To))
                .Where(item => item.Type != AccountHistoryType.OrderClosed);

            var openOrders = await _ordersSnapshotReaderService.GetActiveByAccountIdsAsync(clientAccountIds);

            var historyOrders = (await _ordersHistoryRepository.GetHistoryAsync(request.ClientId, clientAccountIds, request.From, request.To))
                .Where(item => item.Status != OrderStatus.Rejected);

            return AccountNewHistoryBackendResponse.Create(accounts, openOrders, historyOrders);
        }
    }
}
