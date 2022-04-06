// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.SqlRepositories.Entities
{
    public class OperationExecutionInfoWithPauseEntity 
    {
        public OperationExecutionInfoEntity ExecutionInfo { get; set; }
        
        [CanBeNull] public OperationExecutionPauseEntity CurrentPause { get; set; }
        
        [CanBeNull] public OperationExecutionPauseEntity LatestCancelledPause { get; set; }
        
        public long TotalCount { get; set; }
    }
}