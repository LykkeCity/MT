using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using Rocks.Caching;
using System.Linq;

namespace MarginTrading.DataReader.Services.Implementation
{
    internal class OrdersSnapshotReaderService : IOrdersSnapshotReaderService
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly ICacheProvider _cacheProvider;
        private const string BlobName = "orders";

        public OrdersSnapshotReaderService(IMarginTradingBlobRepository blobRepository, ICacheProvider cacheProvider)
        {
            _blobRepository = blobRepository;
            _cacheProvider = cacheProvider;
        }

        public Task<IReadOnlyList<Order>> GetAllAsync()
        {
            return _cacheProvider.GetAsync(nameof(OrdersSnapshotReaderService),
                async () => new CachableResult<IReadOnlyList<Order>>(
                    (IReadOnlyList<Order>) await _blobRepository.ReadAsync<List<Order>>(
                        LykkeConstants.StateBlobContainer, BlobName) ?? Array.Empty<Order>(),
                    CachingParameters.FromSeconds(10)));
        }

        public async Task<IReadOnlyList<Order>> GetActiveAsync()
        {
            return (await GetAllAsync()).Where(o => o.Status == OrderStatus.Active).ToList();
        }

        public async Task<IReadOnlyList<Order>> GetActiveByAccountIdsAsync(string[] accountIds)
        {
            return (await GetAllAsync()).Where(o => o.Status == OrderStatus.Active && accountIds.Contains(o.AccountId)).ToList();
        }

        public async Task<IReadOnlyList<Order>> GetPendingAsync()
        {
            return (await GetAllAsync()).Where(o => o.Status == OrderStatus.WaitingForExecution).ToList();
        }
    }
}