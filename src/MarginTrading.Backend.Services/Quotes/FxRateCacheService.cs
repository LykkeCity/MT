// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Extensions;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

namespace MarginTrading.Backend.Services.Quotes
{
    public class FxRateCacheService : TimerPeriod, IFxRateCacheService
    {
        private readonly ILog _log;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly IEventChannel<FxBestPriceChangeEventArgs> _fxBestPriceChangeEventChannel;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private Dictionary<string, InstrumentBidAskPair> _quotes;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly OrdersCache _ordersCache;
        private const string BlobName = "FxRates";

        public FxRateCacheService(ILog log, 
            IMarginTradingBlobRepository blobRepository,
            IEventChannel<FxBestPriceChangeEventArgs> fxBestPriceChangeEventChannel,
            MarginTradingSettings marginTradingSettings,
            IAssetPairDayOffService assetPairDayOffService,
            OrdersCache ordersCache)
            : base(nameof(FxRateCacheService), marginTradingSettings.BlobPersistence.FxRatesDumpPeriodMilliseconds, log)
        {
            _log = log;
            _blobRepository = blobRepository;
            _fxBestPriceChangeEventChannel = fxBestPriceChangeEventChannel;
            _marginTradingSettings = marginTradingSettings;
            _assetPairDayOffService = assetPairDayOffService;
            _ordersCache = ordersCache;
            _quotes = new Dictionary<string, InstrumentBidAskPair>();
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_quotes.TryGetValue(instrument, out var quote))
                    throw new FxRateNotFoundException(instrument, string.Format(MtMessages.FxRateNotFound, instrument));

                return quote;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public Dictionary<string, InstrumentBidAskPair> GetAllQuotes()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _quotes.ToDictionary(x => x.Key, y => y.Value);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public Task SetQuote(ExternalExchangeOrderbookMessage orderBookMessage)
        {
            var isEodOrderbook = orderBookMessage.ExchangeName == ExternalOrderbookService.EodExternalExchange;

            if (_marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForEodFx && isEodOrderbook ||
                _marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForTradingFx && !isEodOrderbook)
            {
                var isAssetTradingDisabled = _assetPairDayOffService.IsAssetTradingDisabled(orderBookMessage.AssetPairId);
            
                // we should process normal orderbook only if asset is currently tradable
                if (_marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForTradingFx && isAssetTradingDisabled && !isEodOrderbook)
                {
                    return Task.CompletedTask;
                }
            
                // and process EOD orderbook only if asset is currently not tradable
                if (_marginTradingSettings.OrderbookValidation.ValidateInstrumentStatusForEodFx && !isAssetTradingDisabled && isEodOrderbook)
                {
                    _log.WriteWarning("EOD FX quotes processing", "",
                        $"EOD FX quote for {orderBookMessage.AssetPairId} is skipped, because instrument is within trading hours");
                
                    return Task.CompletedTask;
                }
            }
            
            // TODO: added for debugging purposes, to be removed
            if(SnapshotService.IsMakingSnapshotInProgress)
                _log.WriteInfo(nameof(FxRateCacheService), nameof(SetQuote), $"Setting new fx quote for {orderBookMessage.AssetPairId} - {orderBookMessage.ToJson()}");

            var bidAskPair = CreatePair(orderBookMessage);

            if (bidAskPair == null)
            {
                return Task.CompletedTask;
            }

            SetQuote(bidAskPair);
            
            _fxBestPriceChangeEventChannel.SendEvent(this, new FxBestPriceChangeEventArgs(bidAskPair));
            
            return Task.CompletedTask;
        }

        public void SetQuote(InstrumentBidAskPair bidAskPair)
        {
            _lockSlim.EnterWriteLock();
            try
            {

                if (bidAskPair == null)
                {
                    return;
                }

                if (_quotes.ContainsKey(bidAskPair.Instrument))
                {
                    _quotes[bidAskPair.Instrument] = bidAskPair;
                }
                else
                {
                    _quotes.Add(bidAskPair.Instrument, bidAskPair);
                }
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public RemoveQuoteError RemoveQuote(string assetPairId)
        {
            #region Validation

            var positions = _ordersCache.Positions.GetPositionsByFxInstrument(assetPairId).ToList();
            if (positions.Any())
            {
                return RemoveQuoteError.Failure(
                    $"Cannot delete [{assetPairId}] best FX price because there are {positions.Count} opened positions.",
                    RemoveQuoteErrorCode.PositionsOpened);
            }
            
            var orders = _ordersCache.Active.GetOrdersByFxInstrument(assetPairId).ToList();
            if (orders.Any())
            {
                return RemoveQuoteError.Failure(
                    $"Cannot delete [{assetPairId}] best FX price because there are {orders.Count} active orders.",
                    RemoveQuoteErrorCode.OrdersOpened);
            }

            #endregion

            _lockSlim.EnterWriteLock();
            try
            {
                if (_quotes.ContainsKey(assetPairId))
                {
                    _quotes.Remove(assetPairId);
                    return RemoveQuoteError.Success();
                }

                return RemoveQuoteError.Failure(string.Format(MtMessages.QuoteNotFound, assetPairId), RemoveQuoteErrorCode.QuoteNotFound);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        private InstrumentBidAskPair CreatePair(ExternalExchangeOrderbookMessage message)
        {
            if (!ValidateOrderbook(message))
            {
                return null;
            }
            
            var ask = GetBestPrice(true, message.Asks);
            var bid = GetBestPrice(false, message.Bids);

            return ask == null || bid == null
                ? null
                : new InstrumentBidAskPair
                {
                    Instrument = message.AssetPairId,
                    Date = message.Timestamp,
                    Ask = ask.Value,
                    Bid = bid.Value
                };
        }
        
        private decimal? GetBestPrice(bool isBuy, IReadOnlyCollection<VolumePrice> prices)
        {
            if (!prices.Any())
                return null;
            return isBuy
                ? prices.Min(x => x.Price)
                : prices.Max(x => x.Price);
        }
        
        private bool ValidateOrderbook(ExternalExchangeOrderbookMessage orderbook)
        {
            try
            {
                orderbook.AssetPairId.RequiredNotNullOrWhiteSpace("orderbook.AssetPairId");
                orderbook.ExchangeName.RequiredNotNullOrWhiteSpace("orderbook.ExchangeName");
                orderbook.RequiredNotNull(nameof(orderbook));
                
                orderbook.Bids.RequiredNotNullOrEmpty("orderbook.Bids");
                orderbook.Bids.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                orderbook.Bids.RequiredNotNullOrEmptyEnumerable("orderbook.Bids");
                
                orderbook.Asks.RequiredNotNullOrEmpty("orderbook.Asks");
                orderbook.Asks.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                orderbook.Asks.RequiredNotNullOrEmptyEnumerable("orderbook.Asks");

                return true;
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(ExternalExchangeOrderbookMessage), orderbook.ToJson(), e);
                return false;
            }
        }
        
        public override void Start()
        {
            _quotes =
                _blobRepository
                    .Read<Dictionary<string, InstrumentBidAskPair>>(LykkeConstants.StateBlobContainer, BlobName)
                    ?.ToDictionary(d => d.Key, d => d.Value) ??
                new Dictionary<string, InstrumentBidAskPair>();

            base.Start();
        }

        public override Task Execute()
        {
            return DumpToRepository();
        }

        public override void Stop()
        {
            if (Working)
            {
                DumpToRepository().Wait();    
            }
            
            base.Stop();
        }

        private async Task DumpToRepository()
        {
            try
            {
                await _blobRepository.WriteAsync(LykkeConstants.StateBlobContainer, BlobName, GetAllQuotes());
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(FxRateCacheService), "Save fx rates", "", ex);
            }
        }
    }
}