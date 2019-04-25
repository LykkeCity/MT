using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using FluentScheduler;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Scheduling;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Services;
using MoreLinq;

namespace MarginTrading.Backend.Services.Services
{
    /// <inheritdoc />
    public class OvernightMarginService : IOvernightMarginService
    {
        private readonly IDateService _dateService;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly IOvernightMarginParameterContainer _overnightMarginParameterContainer;
        private readonly ILog _log;
        private readonly IEventChannel<MarginCallEventArgs> _marginCallEventChannel;
        private readonly OvernightMarginSettings _overnightMarginSettings;
        
        public OvernightMarginService(
            IDateService dateService,
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService,
            IAccountUpdateService accountUpdateService,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IOvernightMarginParameterContainer overnightMarginParameterContainer,
            ILog log,
            IEventChannel<MarginCallEventArgs> marginCallEventChannel,
            OvernightMarginSettings overnightMarginSettings)
        {
            _dateService = dateService;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _accountUpdateService = accountUpdateService;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _overnightMarginParameterContainer = overnightMarginParameterContainer;
            _log = log;
            _marginCallEventChannel = marginCallEventChannel;
            _overnightMarginSettings = overnightMarginSettings;
        }

        /// <inheritdoc />
        public void ScheduleNext()
        {
            //need to know in which OvernightMarginJobType we are now and the moment of next change
            var platformTradingSchedule = _scheduleSettingsCacheService.GetPlatformTradingSchedule();
            
            _overnightMarginParameterContainer.SetOvernightMarginParameterState(false);
            
            var currentDateTime = _dateService.Now();
            DateTime nextStart;
            (DateTime Warn, DateTime Start, DateTime End) operatingInterval = default;
            
            //no disabled trading schedule items.. doing nothing
            if (platformTradingSchedule.All(x => x.Enabled())
                //OR no disabled intervals in current compilation, schedule recheck
                || !TryGetOperatingInterval(platformTradingSchedule, currentDateTime, out operatingInterval))
            {
                nextStart = currentDateTime.Date.AddDays(1);
            }
            //schedule warning
            else if (currentDateTime < operatingInterval.Warn)
            {   
                nextStart = operatingInterval.Warn;
            }
            //schedule overnight margin parameter start
            else if (currentDateTime < operatingInterval.Start)
            {
                HandleOvernightMarginWarnings();
                nextStart = operatingInterval.Start;
            }
            //schedule overnight margin parameter drop
            else if (currentDateTime < operatingInterval.End)
            {
                _overnightMarginParameterContainer.SetOvernightMarginParameterState(true);
                nextStart = operatingInterval.End;

                PlanEodJob(operatingInterval.Start, currentDateTime);
            }
            else
            {
                _log.WriteFatalErrorAsync(nameof(OvernightMarginService), nameof(ScheduleNext),
                        new Exception(
                            $"Incorrect platform trading schedule! Need to fix it and restart the service. Check time: [{currentDateTime:s}], detected operation interval: [{operatingInterval.ToJson()}]"))
                    .Wait();
                return;
            }
            
            _log.WriteInfo(nameof(OvernightMarginService), nameof(ScheduleNext),
                $"Planning next check to [{nextStart:s}]." 
                + $" Current margin parameter state: [{_overnightMarginParameterContainer.GetOvernightMarginParameterState()}]."
                + $" Check time: [{currentDateTime:s}]."
                + (operatingInterval != default ? $" Detected operation interval: [{operatingInterval.ToJson()}]." : ""));
            JobManager.AddJob(ScheduleNext, (s) => s.NonReentrant().ToRunOnceAt(nextStart));
        }

        private void PlanEodJob(DateTime operatingIntervalStart, DateTime currentDateTime)
        {
            var eodTime = operatingIntervalStart.AddMinutes(_overnightMarginSettings.ActivationPeriodMinutes);

            if (currentDateTime < eodTime)
            {
                JobManager.AddJob(() => _tradingEngine.ProcessExpiredOrders(),
                    (s) => s.NonReentrant().ToRunOnceAt(eodTime));
            }
            else
            {
                JobManager.AddJob(() => _tradingEngine.ProcessExpiredOrders(), 
                    (s) => s.ToRunNow());
            }
        }

        private void HandleOvernightMarginWarnings()
        {
            foreach (var account in _accountsCacheService.GetAll())
            {
                var accountOvernightUsedMargin = _accountUpdateService.CalculateOvernightUsedMargin(account);
                var accountLevel = account.GetAccountLevel(accountOvernightUsedMargin);

                if (accountLevel == AccountLevel.StopOut)
                {
                    _marginCallEventChannel.SendEvent(this, 
                        new MarginCallEventArgs(account, AccountLevel.OvernightMarginCall));
                }
            }
        }

        public bool TryGetOperatingInterval(List<CompiledScheduleTimeInterval> platformTrading,
            DateTime currentDateTime, out (DateTime Warn, DateTime Start, DateTime End) resultingInterval)
        {
            //no disabled intervals ahead => false
            if (!platformTrading.Any(x => x.End > currentDateTime && !x.Enabled()))
            {
                resultingInterval = default;
                return false;
            }

            //add "all-the-time-enabled" time interval with min rank
            platformTrading.Add(new CompiledScheduleTimeInterval(
                new ScheduleSettings { Rank = int.MinValue, IsTradeEnabled = true, }, 
                DateTime.MinValue, DateTime.MaxValue));

            //find current active interval
            var currentActiveInterval = platformTrading
                .Where(x => x.Start <= currentDateTime && x.End > currentDateTime)
                .MaxBy(x => x.Schedule.Rank);

            //find changing time: MIN(end of current, start of next with higher Rank)
            //if the same state => continue
            //      other state => ON to OFF => Warn, Start, End
            //                     OFF to ON => End & Warn, Start => DateTime.Min
            DateTime? endOfInterval = null;
            foreach (var nextWithHigherRank in platformTrading.Where(x => x.Start > currentDateTime
                                                              && x.Start < currentActiveInterval.End
                                                              && x.Schedule.Rank > currentActiveInterval.Schedule.Rank)
                .OrderBy(x => x.Start)
                .ThenByDescending(x => x.Schedule.Rank))
            {   
                //if the state is the same - it changes nothing but shifts start border
                if (currentActiveInterval.Enabled() == nextWithHigherRank.Enabled())
                {
                    endOfInterval = nextWithHigherRank.End;
                    continue;
                }

                //if interval end before the shifted border we can skip it
                if (endOfInterval.HasValue && nextWithHigherRank.End < endOfInterval.Value)
                {
                    continue;
                }

                if (nextWithHigherRank.Enabled())
                {
                    resultingInterval = (DateTime.MinValue, DateTime.MinValue, nextWithHigherRank.Start);
                    return true;
                }

                var resultingStart = endOfInterval ?? nextWithHigherRank.Start;

                resultingInterval = (resultingStart.Subtract(TimeSpan.FromMinutes(
                        _overnightMarginSettings.WarnPeriodMinutes + _overnightMarginSettings.ActivationPeriodMinutes)),
                    resultingStart.Subtract(TimeSpan.FromMinutes(_overnightMarginSettings.ActivationPeriodMinutes)),
                    nextWithHigherRank.End);
                return true;
            }
            
            resultingInterval = (DateTime.MinValue, DateTime.MinValue, currentActiveInterval.End);
            return true;
        }
    }
}