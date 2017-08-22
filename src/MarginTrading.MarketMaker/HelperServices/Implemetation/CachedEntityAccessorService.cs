using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.AzureRepositories;
using Microsoft.WindowsAzure.Storage.Table;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal abstract class CachedEntityAccessorService<TEntity> where TEntity : class, ITableEntity, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly CachingParameters InfiniteCachingParameters = CachingParameters.InfiniteCache;

        private readonly ICacheProvider _cacheProvider;

        private readonly IEntityRepository<TEntity> _repository;
        private readonly string _typeNameKeyPrefix;

        protected CachedEntityAccessorService(ICacheProvider cacheProvider, IEntityRepository<TEntity> repository)
        {
            _typeNameKeyPrefix =
                $"{{{typeof(CachedEntityAccessorService<>).Name}}}{{{typeof(TEntity).AssemblyQualifiedName}}}";
            _cacheProvider = cacheProvider;
            _repository = repository;
        }

        protected virtual CachingParameters CachingParameters => InfiniteCachingParameters;

        protected Task UpdateByKey(EntityKeys keys, Action<TEntity> updateFieldFunc)
        {
            return UpdateByKey(keys, updateFieldFunc, k => new TEntity());
        }

        protected async Task UpdateByKey(EntityKeys keys, Action<TEntity> updateFieldFunc,
            Func<EntityKeys, TEntity> createIfNotExists)
        {
            var currentEntity = await GetByKeyAsync(keys) ?? createIfNotExists(keys);
            updateFieldFunc(currentEntity);
            currentEntity.PartitionKey = keys.PartitionKey;
            currentEntity.RowKey = keys.RowKey;
            await _repository.SetAsync(currentEntity);
            _cacheProvider.Add(GetCacheKey(keys), currentEntity, CachingParameters);
        }

        [CanBeNull]
        protected TEntity GetByKey(EntityKeys keys)
        {
            return _cacheProvider.Get(GetCacheKey(keys),
                () => new CachableResult<TEntity>(_repository.GetAsync(keys.PartitionKey, keys.RowKey).Result,
                    CachingParameters));
        }

        [ItemCanBeNull]
        private Task<TEntity> GetByKeyAsync(EntityKeys keys)
        {
            return _cacheProvider.GetAsync(GetCacheKey(keys),
                async () => new CachableResult<TEntity>(await _repository.GetAsync(keys.PartitionKey, keys.RowKey),
                    CachingParameters));
        }

        private string GetCacheKey(EntityKeys keys)
        {
            return _typeNameKeyPrefix + '{' + keys.PartitionKey + '}' + '{' + keys.RowKey + '}';
        }

        protected struct EntityKeys
        {
            public string PartitionKey { get; }
            public string RowKey { get; }

            public EntityKeys(string partitionKey, string rowKey)
            {
                PartitionKey = partitionKey;
                RowKey = rowKey;
            }
        }
    }
}