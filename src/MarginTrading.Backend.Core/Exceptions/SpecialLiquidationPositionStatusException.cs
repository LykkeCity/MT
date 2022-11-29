// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class SpecialLiquidationPositionStatusException : InvalidOperationException
    {
        private const string DefaultMessage = "Position status is not expected for special liquidation";

        public SpecialLiquidationPositionStatusException(string positionId, PositionStatus positionStatus)
            : base(DefaultMessage)
        {
            PositionId = positionId;
            PositionStatus = positionStatus;
        }

        protected SpecialLiquidationPositionStatusException([NotNull] SerializationInfo info,
            StreamingContext context,
            string positionId,
            PositionStatus positionStatus) :
            base(info, context)
        {
            PositionId = positionId;
            PositionStatus = positionStatus;
        }

        public SpecialLiquidationPositionStatusException([CanBeNull] string message,
            string positionId,
            PositionStatus positionStatus) :
            base(message ?? DefaultMessage)
        {
            PositionId = positionId;
            PositionStatus = positionStatus;
        }

        public SpecialLiquidationPositionStatusException([CanBeNull] string message,
            [CanBeNull] Exception innerException,
            string positionId,
            PositionStatus positionStatus) :
            base(message ?? DefaultMessage, innerException)
        {
            PositionId = positionId;
            PositionStatus = positionStatus;
        }

        public string PositionId { get; }

        public PositionStatus PositionStatus { get; }
    }
}