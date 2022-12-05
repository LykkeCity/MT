// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Extensions
{

    internal delegate bool TryGetPosition(string positionId, out Position result);
    internal delegate InstrumentTradingStatus GetAssetTradingStatus(string assetPairId);
    
    internal static class PositionCloseRequestExtensions
    {
        public static PositionsCloseData Parse(this PositionCloseRequest request, 
            TryGetPosition positionGetter,
            GetAssetTradingStatus tradingStatusGetter,
            string positionId,
            string accountId)
        {
            positionGetter.Invoke(positionId, out var position);
            if (position == null)
            {
                throw new PositionValidationException($"Position [{positionId}] not found",
                    PositionValidationError.PositionNotFound);
            }

            if (position.AccountId != accountId)
            {
                throw new AccountValidationException($"Account mismatch for position [{positionId}]",
                    AccountValidationError.AccountMismatch);
            }

            var assetTradingStatus = tradingStatusGetter.Invoke(position.AssetPairId);
            if (!assetTradingStatus.TradingEnabled)
            {
                throw new InstrumentValidationException(
                    assetTradingStatus.Reason == InstrumentTradingDisabledReason.InstrumentTradingDisabled
                        ? InstrumentValidationError.InstrumentTradingDisabled
                        : InstrumentValidationError.TradesAreNotAvailable);
            }

            var originator = request?.Originator.ToType<OriginatorType>() ?? OriginatorType.Investor;

            return new PositionsCloseData(
                position,
                position.AccountId,
                position.AssetPairId,
                position.OpenMatchingEngineId,
                position.ExternalProviderId,
                originator,
                request?.AdditionalInfo,
                position.EquivalentAsset);
        }
    }
}