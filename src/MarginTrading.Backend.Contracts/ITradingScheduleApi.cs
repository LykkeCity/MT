using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradingSchedule;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// Api to retrieve compiled trading schedule - for cache initialization only.
    /// </summary>
    [PublicAPI]
    public interface ITradingScheduleApi
    {
        /// <summary>
        /// Get current compiled trading schedule for each asset pair in a form of the list of time intervals.
        /// Cache is invalidated and recalculated after 00:00:00.000 each day on request. 
        /// </summary>
        [Get("/api/trading-schedule/compiled")]
        Task<Dictionary<string, List<CompiledScheduleTimeIntervalContract>>> CompiledTradingSchedule();

        /// <summary>
        /// Get current instrument's trading status.
        /// Do not use this endpoint from FrontEnd!
        /// </summary>
        /// <param name="assetPairId"></param>
        [Get("/api/trading-schedule/is-enabled/{assetPairId}")]
        Task<bool> IsInstrumentEnabled(string assetPairId);

        /// <summary>
        /// Get current trading status of all instruments.
        /// Do not use this endpoint from FrontEnd!
        /// </summary>
        [Get("/api/trading-schedule/are-enabled")]
        Task<Dictionary<string, bool>> AreInstrumentsEnabled();
    }
}