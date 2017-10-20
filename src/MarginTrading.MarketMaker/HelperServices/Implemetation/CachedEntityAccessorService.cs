using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.AzureRepositories;
using Microsoft.WindowsAzure.Storage.Table;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal class CachedEntityAccessorService<TEntity> : CachedEntityAccessorService
        where TEntity : class, ITableEntity, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly CachingParameters InfiniteCachingParameters = CachingParameters.InfiniteCache;

        private readonly ICacheProvider _cacheProvider;
        private readonly string _typeNameKeyPrefix;

        private IAbstractRepository<TEntity> _repository { get; }

        public CachedEntityAccessorService(ICacheProvider cacheProvider, IAbstractRepository<TEntity> repository)
        {
            _typeNameKeyPrefix =
                $"{{{typeof(CachedEntityAccessorService<>).Name}}}{{{typeof(TEntity).AssemblyQualifiedName}}}";
            _cacheProvider = cacheProvider;
            _repository = repository;
        }

        protected virtual CachingParameters CachingParameters => InfiniteCachingParameters;

        public Task UpdateByKeyAsync(EntityKeys keys, Action<TEntity> updateFieldFunc)
        {
            return UpdateByKeyAsync(keys, updateFieldFunc, k => new TEntity());
        }

        public void DeleteByKey(EntityKeys keys)
        {
            _cacheProvider.Remove(GetCacheKey(keys));
        }

        public async Task UpdateByKeyAsync(EntityKeys keys, Action<TEntity> updateFieldFunc,
            Func<EntityKeys, TEntity> createIfNotExists)
        {
            var currentEntity = await GetByKeyAsync(keys) ?? createIfNotExists(keys);
            updateFieldFunc(currentEntity);
            currentEntity.PartitionKey = keys.PartitionKey;
            currentEntity.RowKey = keys.RowKey;
            await _repository.InsertOrReplaceAsync(currentEntity);
            _cacheProvider.Add(GetCacheKey(keys), currentEntity, CachingParameters);
        }

        public async Task Upsert(TEntity entity)
        {
            await _repository.InsertOrReplaceAsync(entity);
            _cacheProvider.Add(GetCacheKey(new EntityKeys(entity.PartitionKey, entity.RowKey)), entity, CachingParameters);
        }

        [CanBeNull]
        public TEntity GetByKey(EntityKeys keys)
        {
            return _cacheProvider.Get(GetCacheKey(keys),
                () => new CachableResult<TEntity>(_repository.GetAsync(keys.PartitionKey, keys.RowKey).GetAwaiter().GetResult(),
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
    }

    internal class CachedEntityAccessorService
    {
        public struct EntityKeys
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