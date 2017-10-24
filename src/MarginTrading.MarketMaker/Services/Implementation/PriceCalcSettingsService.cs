using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;
using MarginTrading.MarketMaker.Models.Api;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    class PriceCalcSettingsService : IPriceCalcSettingsService
    {
        private readonly ReadWriteLockedDictionary<string, ImmutableDictionary<string, ExchangeExtPriceSettingsEntity>> _exchangesCache
            = new ReadWriteLockedDictionary<string, ImmutableDictionary<string, ExchangeExtPriceSettingsEntity>>();
        private readonly CachedEntityAccessorService<AssetPairExtPriceSettingsEntity> _assetPairsCachedAccessor;
        private readonly IExchangeExtPriceSettingsRepository _exchangesRepository;
        private readonly IAssetsPairsExtPriceSettingsRepository _assetPairsRepository;

        public PriceCalcSettingsService(ICacheProvider cache, IAssetsPairsExtPriceSettingsRepository assetPairsRepository, IExchangeExtPriceSettingsRepository exchangesRepository)
        {
            _exchangesRepository = exchangesRepository;
            _assetPairsRepository = assetPairsRepository;
            _assetPairsCachedAccessor = new CachedEntityAccessorService<AssetPairExtPriceSettingsEntity>(cache, assetPairsRepository);
        }

        public bool IsStepEnabled(OrderbookGeneratorStepEnum step, string assetPairId)
        {
            return Asset(assetPairId).Steps.GetValueOrDefault(step, true);
        }

        public string GetPresetPrimaryExchange(string assetPairId)
        {
            return Asset(assetPairId).PresetDefaultExchange;
        }

        public decimal GetVolumeMultiplier(string assetPairId, string exchangeName)
        {
            return (decimal)Exchange(assetPairId, exchangeName).OrderGeneration.VolumeMultiplier;
        }

        public TimeSpan GetOrderbookOutdatingThreshold(string assetPairId, string exchangeName, DateTime now)
        {
            return Exchange(assetPairId, exchangeName).OrderbookOutdatingThreshold;
        }

        public RepeatedOutliersParams GetRepeatedOutliersParams(string assetPairId)
        {
            var p = Asset(assetPairId).RepeatedOutliers;
            return new RepeatedOutliersParams(p.MaxSequenceLength, p.MaxSequenceAge, (decimal) p.MaxAvg, p.MaxAvgAge);
        }

        public decimal GetOutlierThreshold(string assetPairId)
        {
            return (decimal) Asset(assetPairId).OutlierThreshold;
        }

        public ImmutableDictionary<string, decimal> GetHedgingPreferences(string assetPairId)
        {
            return AllExchanges(assetPairId).ToImmutableDictionary(e => e.Key, e => e.Value.Hedging.IsTemporarilyUnavailable ? 0m : (decimal)e.Value.Hedging.DefaultPreference);
        }

        public (decimal Bid, decimal Ask) GetPriceMarkups(string assetPairId)
        {
            var markups = Asset(assetPairId).Markups;
            return ((decimal)markups.Bid, (decimal)markups.Ask);
        }

        public IReadOnlyList<HedgingPreferenceModel> GetAllHedgingPreferences()
        {
            return _exchangesCache.Values.SelectMany(ap =>
                ap.Values.Select(e => new HedgingPreferenceModel
                {
                    AssetPairId = e.AssetPairId,
                    Exchange = e.Exchange,
                    Preference = (decimal) e.Hedging.DefaultPreference,
                    HedgingTemporarilyDisabled = e.Hedging.IsTemporarilyUnavailable,
                })).ToList();
        }

        public async Task<IReadOnlyList<AssetPairExtPriceSettingsModel>> GetAllAsync(string assetPairId = null)
        {
            IList<AssetPairExtPriceSettingsEntity> assetPairsEntities;
            if (assetPairId == null)
                assetPairsEntities = await _assetPairsRepository.GetAllAsync();
            else
                assetPairsEntities = new[] {_assetPairsCachedAccessor.GetByKey(GetAssetPairKeys(assetPairId))};

            return assetPairsEntities
                .GroupJoin(await _exchangesRepository.GetAllAsync(), ap => ap.AssetPairId, e => e.AssetPairId,
                    (assetPair, exchange) => new AssetPairExtPriceSettingsModel
                    {
                        AssetPairId = assetPair.AssetPairId,
                        Timestamp = assetPair.Timestamp,
                        PresetDefaultExchange = assetPair.PresetDefaultExchange,
                        RepeatedOutliers = new RepeatedOutliersParamsModel
                        {
                            MaxSequenceLength = assetPair.RepeatedOutliers.MaxSequenceLength,
                            MaxSequenceAge = assetPair.RepeatedOutliers.MaxSequenceAge,
                            MaxAvg = (decimal) assetPair.RepeatedOutliers.MaxAvg,
                            MaxAvgAge = assetPair.RepeatedOutliers.MaxAvgAge,
                        },
                        Markups = new MarkupsModel
                        {
                            Bid = (decimal) assetPair.Markups.Bid,
                            Ask = (decimal) assetPair.Markups.Ask,
                        },
                        OutlierThreshold = assetPair.OutlierThreshold,
                        Steps = assetPair.Steps,
                        Exchanges = exchange.Select(e => new ExchangeExtPriceSettingsModel
                        {
                            Exchange = e.Exchange,
                            Hedging = new HedgingSettingsModel
                            {
                                DefaultPreference = e.Hedging.DefaultPreference,
                                IsTemporarilyUnavailable = e.Hedging.IsTemporarilyUnavailable,
                            },
                            OrderGeneration = new OrderGenerationSettingsModel
                            {
                                VolumeMultiplier = (decimal) e.OrderGeneration.VolumeMultiplier,
                                OrderRenewalDelay = e.OrderGeneration.OrderRenewalDelay,
                            },
                            OrderbookOutdatingThreshold = e.OrderbookOutdatingThreshold,
                            Disabled = new DisabledSettingsModel
                            {
                                IsTemporarilyDisabled = e.Disabled.IsTemporarilyDisabled,
                                Reason = e.Disabled.Reason,
                            }
                        }).ToList()
                    }).ToList();
        }

        public Task Set(AssetPairExtPriceSettingsModel model)
        {
            var entity = new AssetPairExtPriceSettingsEntity
            {
                AssetPairId = model.AssetPairId,
                Timestamp = model.Timestamp,
                PresetDefaultExchange = model.PresetDefaultExchange,
                RepeatedOutliers = new AssetPairExtPriceSettingsEntity.RepeatedOutliersParams
                {
                    MaxSequenceLength = model.RepeatedOutliers.MaxSequenceLength,
                    MaxSequenceAge = model.RepeatedOutliers.MaxSequenceAge,
                    MaxAvg = (double) model.RepeatedOutliers.MaxAvg,
                    MaxAvgAge = model.RepeatedOutliers.MaxAvgAge,
                },
                Markups = new AssetPairExtPriceSettingsEntity.MarkupsParams
                {
                    Bid = (double)model.Markups.Bid,
                    Ask = (double)model.Markups.Ask,
                },
                OutlierThreshold = model.OutlierThreshold,
                Steps = model.Steps,
            };

            var upsertAssetPairTask = _assetPairsCachedAccessor.Upsert(entity);

            var exchangesEntities = model.Exchanges.Select(e => new ExchangeExtPriceSettingsEntity
            {
                Exchange = e.Exchange,
                AssetPairId = model.AssetPairId,
                Hedging = new ExchangeExtPriceSettingsEntity.HedgingSettings
                {
                    DefaultPreference = e.Hedging.DefaultPreference,
                    IsTemporarilyUnavailable = e.Hedging.IsTemporarilyUnavailable,
                },
                OrderGeneration = new ExchangeExtPriceSettingsEntity.OrderGenerationSettings
                {
                    VolumeMultiplier = (double) e.OrderGeneration.VolumeMultiplier,
                    OrderRenewalDelay = e.OrderGeneration.OrderRenewalDelay,
                },
                OrderbookOutdatingThreshold = e.OrderbookOutdatingThreshold,
                Disabled = new ExchangeExtPriceSettingsEntity.DisabledSettings
                {
                    IsTemporarilyDisabled = e.Disabled.IsTemporarilyDisabled,
                    Reason = e.Disabled.Reason,
                }
            }).ToImmutableDictionary(e => e.Exchange);

            _exchangesCache.AddOrUpdate(entity.AssetPairId,
                    k =>
                    {
                        _exchangesRepository.InsertOrReplaceAsync(exchangesEntities.Values).GetAwaiter().GetResult();
                        return exchangesEntities;
                    },
                    (k, old) =>
                    {
                        Task.WaitAll(
                            _exchangesRepository.InsertOrReplaceAsync(exchangesEntities.Values),
                            _exchangesRepository.DeleteAsync(old.Values.Where(o =>
                                !exchangesEntities.ContainsKey(o.Exchange))));
                        return exchangesEntities;
                    });

            return upsertAssetPairTask;
        }


        private ImmutableDictionary<string, ExchangeExtPriceSettingsEntity> AllExchanges(string assetPairId)
        {
            return _exchangesCache.GetOrAdd(assetPairId,
                k => _exchangesRepository.GetAsync(k).GetAwaiter().GetResult().ToImmutableDictionary(e => e.Exchange));
        }

        private ExchangeExtPriceSettingsEntity Exchange(string assetPairId, string exchange)
        {
            return AllExchanges(assetPairId).GetValueOrDefault(exchange)
                   ?? throw new InvalidOperationException($"Settings for exchange {exchange} for asset pair {assetPairId} not found");
        }

        private AssetPairExtPriceSettingsEntity Asset(string assetPairId)
        {
            return _assetPairsCachedAccessor.GetByKey(GetAssetPairKeys(assetPairId))
                   ?? throw new InvalidOperationException($"Settings for asset pair {assetPairId} not found");
        }

        private static CachedEntityAccessorService.EntityKeys GetAssetPairKeys(string assetPairId)
        {
            return new CachedEntityAccessorService.EntityKeys(AssetPairExtPriceSettingsEntity.GeneratePartitionKey(), AssetPairExtPriceSettingsEntity
                .GenerateRowKey(assetPairId));
        }
    }
}