using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.AzureRepositories;
using Microsoft.WindowsAzure.Storage.Table;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal abstract class CachedEntityAccessorService<TEntity> where TEntity: class, ITableEntity, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly CachingParameters InfiniteCachingParameters = CachingParameters.InfiniteCache;

        protected readonly IEntityRepository<TEntity> _repository;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _typeNameKeyPrefix;

        protected CachedEntityAccessorService(ICacheProvider cacheProvider, IEntityRepository<TEntity> repository)
        {
            _typeNameKeyPrefix = $"{{{typeof(CachedEntityAccessorService<>).Name}}}{{{typeof(TEntity).AssemblyQualifiedName}}}";
            _cacheProvider = cacheProvider;
            _repository = repository;
        }

        protected Task UpdateByKey((string PartitionKey, string RowKey) keys, Action<TEntity> updateFieldFunc)
            => UpdateByKey(keys, updateFieldFunc, k => new TEntity());

        protected async Task UpdateByKey((string PartitionKey, string RowKey) keys, Action<TEntity> updateFieldFunc, Func<(string PartitionKey, string RowKey), TEntity> createIfNotExists)
        {
            var currentEntity = await GetByKeyAsync(keys) ?? createIfNotExists(keys);
            updateFieldFunc(currentEntity);
            (currentEntity.PartitionKey, currentEntity.RowKey) = keys;
            await _repository.SetAsync(currentEntity);
            _cacheProvider.Add(GetCacheKey(keys), currentEntity, CachingParameters);
        }

        protected virtual CachingParameters CachingParameters => InfiniteCachingParameters;

        [CanBeNull]
        protected TEntity GetByKey((string PartitionKey, string RowKey) keys)
        {
            return _cacheProvider.Get(GetCacheKey(keys), () => new CachableResult<TEntity>(_repository.GetAsync(keys.PartitionKey, keys.RowKey).Result, CachingParameters));
        }

        [ItemCanBeNull]
        private Task<TEntity> GetByKeyAsync((string PartitionKey, string RowKey) keys)
        {
            return _cacheProvider.GetAsync(GetCacheKey(keys), async () => new CachableResult<TEntity>(await _repository.GetAsync(keys.PartitionKey, keys.RowKey), CachingParameters));
        }

        private string GetCacheKey((string PartitionKey, string RowKey) keys) => _typeNameKeyPrefix + '{' + keys.PartitionKey + '}' + '{' + keys.RowKey + '}';
    }
}
