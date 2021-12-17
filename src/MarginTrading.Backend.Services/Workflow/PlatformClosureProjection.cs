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
    public class PlatformClosureProjection
    {
        private readonly ISnapshotService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;

        public PlatformClosureProjection(ISnapshotService snapshotService, ILog log, IIdentityGenerator identityGenerator)
        {
            _snapshotService = snapshotService;
            _log = log;
            _identityGenerator = identityGenerator;
        }

        public async Task Handle(MarketStateChangedEvent e)
        {
            if (!e.IsPlatformClosureEvent())
                return;

            await _log.WriteInfoAsync(nameof(PlatformClosureProjection), nameof(Handle), e.ToJson(),
                "Platform is being closed. Starting trading snapshot backup");

            var result = string.Empty;
            //var result = await _snapshotService.MakeTradingDataSnapshot(e.EventTimestamp.Date, _identityGenerator.GenerateGuid());

            await _log.WriteInfoAsync(nameof(PlatformClosureProjection), nameof(Handle), e.ToJson(), result);
        }
    }   
}