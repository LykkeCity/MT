using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.DayOffSettings
{
    public class CompiledExclusionsListContract
    {
        public IReadOnlyList<CompiledExclusionContract> TradesEnabled { get; set; }
        public IReadOnlyList<CompiledExclusionContract> TradesDisabled { get; set; }
    }
}