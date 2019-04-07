using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services
{
    public class DeduplicationManager : TimerPeriod
    {
        private const string ValueKey = "TradingEngine:DeduplicationTimestamp";
        
        private readonly IDateService _dateService;
        private readonly IDatabase _redisDatabase;
        private readonly MarginTradingSettings _marginTradingSettings;

        public DeduplicationManager(IDateService dateService,
            ILog log,
            MarginTradingSettings marginTradingSettings)
            : base(nameof(DeduplicationManager),
                (int) marginTradingSettings.DeduplicationTimestampPeriod.TotalMilliseconds,
                log)
        {
            _dateService = dateService;
            _redisDatabase = ConnectionMultiplexer.Connect(marginTradingSettings.RedisSettings.Configuration)
                .GetDatabase();
            _marginTradingSettings = marginTradingSettings;
        }

        public override void Start()
        {
            var now = _dateService.Now();
            var lastTimestamp = DateTime.TryParse(_redisDatabase.StringGet(ValueKey), out var lastDt)
                ? lastDt
                : (DateTime?)null;

            if (lastTimestamp != null && lastTimestamp > now.Subtract(
                    _marginTradingSettings.DeduplicationTimestampPeriod))
            {
                throw new Exception("Trading Engine failed to start due to deduplication validation failure");
            }
            
            base.Start();
        }

        public override async Task Execute()
        {
            await _redisDatabase.StringSetAsync(ValueKey, $"{_dateService.Now():s}");
        }
    }
}