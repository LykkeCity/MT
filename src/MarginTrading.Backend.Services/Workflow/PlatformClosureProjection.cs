// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class PlatformClosureProjection
    {
        private readonly ISnapshotService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;
        private readonly IDateService _dateService;

        public PlatformClosureProjection(ISnapshotService snapshotService,
            ILog log,
            IIdentityGenerator identityGenerator,
            IDateService dateService)
        {
            _snapshotService = snapshotService;
            _log = log;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
        }

        [UsedImplicitly]
        public async Task Handle(MarketStateChangedEvent e)
        {
            if (!e.IsPlatformClosureEvent())
                return;
            
            var tradingDay = DateOnly.FromDateTime(e.EventTimestamp);

            string result; 
            try
            {
                result = await _snapshotService.MakeTradingDataSnapshot(e.EventTimestamp.Date,
                    _identityGenerator.GenerateGuid(),
                    SnapshotStatus.Draft);
            }
            catch (Exception ex)
            {
                await _log.WriteWarningAsync(nameof(PlatformClosureProjection),
                    nameof(Handle),
                    e.ToJson(),
                    $"Failed to make trading data draft snapshot for [{tradingDay}]", ex);
                
                if (tradingDay < _dateService.NowDateOnly())
                {
                    await _log.WriteWarningAsync(nameof(PlatformClosureProjection),
                        nameof(Handle),
                        e.ToJson(),
                        "The event is for the past date, so the snapshot draft will not be created.", ex);
                    return;
                }

                throw;
            }

            if (!string.IsNullOrWhiteSpace(result))
            {
                await _log.WriteInfoAsync(nameof(PlatformClosureProjection),
                    nameof(Handle),
                    e.ToJson(),
                    result);
            }
        }
    }   
}