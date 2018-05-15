using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;

namespace MarginTrading.DataReader.Services.Implementation
{
    internal class QuotesSnapshotReadersService : IQuotesSnapshotReadersService, IStartable, IDisposable
    {
        private const string BlobName = "Quotes";
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly TimerTrigger _timerTrigger;
        private IReadOnlyDictionary<string, InstrumentBidAskPair> _snapshot;

        public QuotesSnapshotReadersService(ILog log, IMarginTradingBlobRepository blobRepository)
        {
            _blobRepository = blobRepository;
            _timerTrigger = new TimerTrigger(nameof(QuotesSnapshotReadersService), TimeSpan.FromSeconds(10), log, OnTimerTriggered);
        }

        private async Task OnTimerTriggered(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationtoken)
        {
            _snapshot = await _blobRepository.ReadAsync<Dictionary<string, InstrumentBidAskPair>>(
                            LykkeConstants.StateBlobContainer, BlobName) ??
                        new Dictionary<string, InstrumentBidAskPair>();
        }

        public IReadOnlyDictionary<string, InstrumentBidAskPair> GetSnapshotAsync()
        {
            return _snapshot;
        }

        public void Start()
        {
            _timerTrigger.Start();
        }

        public void Dispose()
        {
            _timerTrigger.Dispose();
        }
    }
}