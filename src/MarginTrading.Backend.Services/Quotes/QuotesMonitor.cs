using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Enums;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Quotes
{
    public class QuotesMonitor : TimerPeriod
    {
        private readonly ILog _log;
        private readonly ISlackNotificationsSender _slackNotificationsSender;
        private readonly MarginSettings _marginSettings;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IDateService _dateService;
        private readonly IAssetPairDayOffService _dayOffService;

        private const int DefaultMaxQuoteAgeInSeconds = 300;
        private const int NotificationRepeatTimeoutInMinutes = 60;
        
        private readonly Dictionary<string, OutdatedQuoteInfo> _outdatedQuotes;

        public QuotesMonitor(ILog log, 
            ISlackNotificationsSender slackNotificationsSender,
            MarginSettings marginSettings,
            IQuoteCacheService quoteCacheService,
            IDateService dateService,
            IAssetPairDayOffService dayOffService) 
            : base("QuotesMonitor", 60000, log)
        {
            _log = log;
            _slackNotificationsSender = slackNotificationsSender;
            _marginSettings = marginSettings;
            _quoteCacheService = quoteCacheService;
            _dateService = dateService;
            _dayOffService = dayOffService;
            _outdatedQuotes = new Dictionary<string, OutdatedQuoteInfo>();
        }

        public override async Task Execute()
        {
            var maxQuoteAgeInSeconds = _marginSettings.MaxMarketMakerLimitOrderAge >= 0
                ? _marginSettings.MaxMarketMakerLimitOrderAge
                : DefaultMaxQuoteAgeInSeconds;
            
            var now = _dateService.Now();
            var minQuoteDateTime = now.AddSeconds(-maxQuoteAgeInSeconds);
            var minNotificationRepeatDate = now.AddMinutes(-NotificationRepeatTimeoutInMinutes);
            
            var quotes = _quoteCacheService.GetAllQuotes();
            
            foreach (var quote in quotes)
            {
                if (_dayOffService.IsDayOff(quote.Key))
                    continue;
                
                if (_outdatedQuotes.TryGetValue(quote.Key, out var info))
                {
                    if (info.LastNotificationSend < minNotificationRepeatDate)
                    {
                        await NotifyQuoteIsOutdated(quote.Value);
                    }
                    
                    continue;
                }

                if (quote.Value.Date <= minQuoteDateTime)
                {
                    await NotifyQuoteIsOutdated(quote.Value);
                }
                else
                {
                    if (_outdatedQuotes.ContainsKey(quote.Key))
                    {
                        await NotifyQuoteIsOk(quote.Value);
                    }
                }
            }
        }

        private async Task NotifyQuoteIsOutdated(InstrumentBidAskPair quote)
        {
            var message = $"Quotes for {quote.Instrument} stopped at {quote.Date}!";

            await _log.WriteInfoAsync(nameof(QuotesMonitor), quote.ToJson(), message);
            await _slackNotificationsSender.SendAsync(ChannelTypes.MtMmRisks, nameof(QuotesMonitor), message);

            var info = new OutdatedQuoteInfo
            {
                LastQuoteRecieved = quote.Date,
                LastNotificationSend = _dateService.Now()
            };
            
            _outdatedQuotes[quote.Instrument] = info;
        }
        
        private async Task NotifyQuoteIsOk(InstrumentBidAskPair quote)
        {
            var message = $"Quotes for {quote.Instrument} started at {quote.Date}";

            await _log.WriteInfoAsync(nameof(QuotesMonitor), quote.ToJson(), message);
            await _slackNotificationsSender.SendAsync(ChannelTypes.MtMmRisks, nameof(QuotesMonitor), message);

            _outdatedQuotes.Remove(quote.Instrument);
        }

        private class OutdatedQuoteInfo
        {
            public DateTime LastQuoteRecieved { get; set; }
            public DateTime LastNotificationSend { get; set; }
        }
    }
}