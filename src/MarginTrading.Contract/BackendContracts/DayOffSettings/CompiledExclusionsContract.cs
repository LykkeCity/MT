using System.Collections.Generic;

namespace MarginTrading.Contract.BackendContracts.DayOffSettings
{
    public class CompiledExclusionsListContract
    {
        public IReadOnlyList<CompiledExclusionContract> TradesEnabled { get; set; }
        public IReadOnlyList<CompiledExclusionContract> TradesDisabled { get; set; }
    }
}