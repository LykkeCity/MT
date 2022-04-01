// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.SqlRepositories.Entities
{
    public class OperationExecutionInfoWithPauseEntity 
    {
        public OperationExecutionInfoEntity ExecutionInfo { get; set; }
        
        public OperationExecutionPauseEntity CurrentPause { get; set; }
        
        public OperationExecutionPauseEntity LatestCancelledPause { get; set; }
    }
}