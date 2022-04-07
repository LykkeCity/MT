// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Snow.Common;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.Products;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Backend.Services.Workflow;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class ProductChangedProjectionTests
    {
        private Mock<ITradingEngine> _tradingEngine;
        private Mock<IAssetPairsCache> _assetPairsCache;
        private Mock<IOrderReader> _orderReader;
        private Mock<IScheduleSettingsCacheService> _scheduleSettingsCacheService;
        private Mock<ITradingInstrumentsManager> _tradingInstrumentsManager;
        private Mock<IRfqService> _rfqService;
        private Mock<IRfqPauseService> _rfqPauseService;
        private Mock<ILog> _log;
        private Mock<IQuoteCacheService> _quoteCache;
        private MarginTradingSettings _mtSettings;

        private const string ProductId = "productId";

        [SetUp]
        public void SetUp()
        {
            _tradingEngine = new Mock<ITradingEngine>();
            _assetPairsCache = new Mock<IAssetPairsCache>();
            _orderReader = new Mock<IOrderReader>();
            _scheduleSettingsCacheService = new Mock<IScheduleSettingsCacheService>();
            _tradingInstrumentsManager = new Mock<ITradingInstrumentsManager>();
            _rfqService = new Mock<IRfqService>();
            _rfqPauseService = new Mock<IRfqPauseService>();
            _mtSettings = new MarginTradingSettings()
            {
                DefaultLegalEntitySettings = new DefaultLegalEntitySettings()
                {
                    DefaultLegalEntity = "default",
                }
            };
            _log = new Mock<ILog>();
            _quoteCache = new Mock<IQuoteCacheService>();
        }

        [Test]
        public async Task TradingDisabled_RfqPaused()
        {
            //Arrange
            var projection = InitProjection();

            var rfqId = Guid.NewGuid().ToString();
            var rfq = new RfqWithPauseSummary()
            {
                Id = rfqId,
            };
            _rfqService.SetupSequence(x => x.GetAsync(It.IsAny<RfqFilter>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<RfqWithPauseSummary>(new List<RfqWithPauseSummary>() {rfq}, 0, 1, 1))
                .ReturnsAsync(new PaginatedResponse<RfqWithPauseSummary>(new List<RfqWithPauseSummary>(), 0, 0, 1));

            var @event = GetTradingDisabledChangedEvent(false, true);

            //Action
            await projection.Handle(@event);

            //Assert
            _rfqPauseService.Verify(x => x.AddAsync(rfqId,
                    PauseSource.TradingDisabled,
                    It.IsAny<Initiator>()),
                Times.Once);
        }

        [Test]
        public async Task TradingEnabled_RfqResumed()
        {
            //Arrange
            var projection = InitProjection();

            var rfqId = Guid.NewGuid().ToString();
            var rfq = new RfqWithPauseSummary()
            {
                Id = rfqId,
                PauseSummary = new RfqPauseSummary()
                {
                    CanBeResumed = true,
                }
            };
            _rfqService.SetupSequence(x => x.GetAsync(It.IsAny<RfqFilter>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<RfqWithPauseSummary>(new List<RfqWithPauseSummary>() {rfq}, 0, 1, 1))
                .ReturnsAsync(new PaginatedResponse<RfqWithPauseSummary>(new List<RfqWithPauseSummary>(), 0, 0, 1));

            var @event = GetTradingDisabledChangedEvent(true, false);

            //Action
            await projection.Handle(@event);

            //Assert
            _rfqPauseService.Verify(x => x.ResumeAsync(rfqId,
                    PauseCancellationSource.TradingEnabled,
                    It.IsAny<Initiator>()),
                Times.Once);
        }
        
        [Test]
        public async Task TradingEnabled_RfqPauseStopped()
        {
            //Arrange
            var projection = InitProjection();

            var rfqId = Guid.NewGuid().ToString();
            var rfq = new RfqWithPauseSummary()
            {
                Id = rfqId,
                PauseSummary = new RfqPauseSummary()
                {
                    CanBeStopped = true,
                }
            };
            _rfqService.SetupSequence(x => x.GetAsync(It.IsAny<RfqFilter>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResponse<RfqWithPauseSummary>(new List<RfqWithPauseSummary>() {rfq}, 0, 1, 1))
                .ReturnsAsync(new PaginatedResponse<RfqWithPauseSummary>(new List<RfqWithPauseSummary>(), 0, 0, 1));

            var @event = GetTradingDisabledChangedEvent(true, false);

            //Action
            await projection.Handle(@event);

            //Assert
            _rfqPauseService.Verify(x => x.StopPendingAsync(rfqId,
                    PauseCancellationSource.TradingEnabled,
                    It.IsAny<Initiator>()),
                Times.Once);
        }

        private ProductChangedProjection InitProjection()
        {
            return new ProductChangedProjection(_tradingEngine.Object,
                _assetPairsCache.Object,
                _orderReader.Object,
                _scheduleSettingsCacheService.Object,
                _tradingInstrumentsManager.Object,
                _rfqService.Object,
                _rfqPauseService.Object,
                _mtSettings,
                _log.Object,
                _quoteCache.Object);
        }

        private ProductChangedEvent GetTradingDisabledChangedEvent(bool oldTradingDisabled, bool newTradingDisabled)
        {
            return new ProductChangedEvent()
            {
                ChangeType = ChangeType.Edition,
                OldValue = new ProductContract()
                {
                    ProductId = ProductId,
                    Name = ProductId,
                    TradingCurrency = "EUR",
                    IsStarted = true,
                    IsTradingDisabled = oldTradingDisabled,
                },
                NewValue = new ProductContract()
                {
                    ProductId = ProductId,
                    Name = ProductId,
                    TradingCurrency = "EUR",
                    IsStarted = true,
                    IsTradingDisabled = newTradingDisabled,
                },
                Username = "user",
            };
        }
    }
}