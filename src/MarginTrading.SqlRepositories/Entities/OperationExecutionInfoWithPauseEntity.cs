// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.SqlRepositories.Entities
{
    public class OperationExecutionInfoWithPauseEntity 
    {
        public OperationExecutionInfoEntity ExecutionInfo { get; set; }
        
        public OperationExecutionPauseEntity CurrentPause { get; set; }
        
        public OperationExecutionPauseEntity LatestCancelledPause { get; set; }

        public static readonly Func<OperationExecutionInfoEntity,
            OperationExecutionPauseEntity,
            OperationExecutionPauseEntity,
            OperationExecutionInfoWithPauseEntity> ComposeFunc =
            (executionInfo, currentPause, latestCancelledPause) =>
                new OperationExecutionInfoWithPauseEntity
                {
                    ExecutionInfo = executionInfo,
                    CurrentPause = currentPause.Oid.HasValue ? currentPause : null,
                    LatestCancelledPause = latestCancelledPause.Oid.HasValue ? latestCancelledPause : null
                };

        public static readonly string DapperSplitOn = "Oid,Oid";
    }
}