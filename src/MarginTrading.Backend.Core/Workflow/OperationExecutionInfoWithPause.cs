// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public class OperationExecutionInfoWithPause<T> : OperationExecutionInfo<T> where T : class
    {
        /// <summary>
        /// Current non-cancelled pause (either pending, or active or pending cancellation)
        /// </summary>
        public OperationExecutionPause CurrentPause { get; set; }
        
        /// <summary>
        /// Latest cancelled pause
        /// </summary>
        public OperationExecutionPause LatestCancelledPause { get; set; }
        
        public OperationExecutionInfoWithPause([NotNull] string operationName, [NotNull] string id, DateTime lastModified, [NotNull] T data) : base(operationName, id, lastModified, data)
        {
        }
    }
}