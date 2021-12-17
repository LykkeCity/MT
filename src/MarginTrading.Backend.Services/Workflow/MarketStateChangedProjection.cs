// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Extensions;

namespace MarginTrading.Backend.Services.Workflow
{
    public class MarketStateChangedProjection
    {
        private readonly ISnapshotService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;

        public MarketStateChangedProjection(ISnapshotService snapshotService, ILog log, IIdentityGenerator identityGenerator)
        {
            _snapshotService = snapshotService;
            _log = log;
            _identityGenerator = identityGenerator;
        }

        public async Task Handle(MarketStateChangedEvent e)
        {
            await _log.WriteInfoAsync(nameof(MarketStateChangedProjection), nameof(Handle), e.ToJson(),
                $"Received {nameof(MarketStateChangedEvent)} event");
            
            if (!e.IsPlatformClosureEvent())
                return;

            await _log.WriteInfoAsync(nameof(MarketStateChangedProjection), nameof(Handle), e.ToJson(),
                "Platform is being closed. Starting trading snapshot backup");

            //var result = await _snapshotService.MakeTradingDataSnapshot(e.EventTimestamp.Date, _identityGenerator.GenerateGuid());

            await _log.WriteInfoAsync(nameof(MarketStateChangedProjection), nameof(Handle), e.ToJson(), result);
        }
    }   
}