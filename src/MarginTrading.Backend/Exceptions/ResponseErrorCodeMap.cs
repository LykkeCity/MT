// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Exceptions
{
    public static class ResponseErrorCodeMap
    {
        public const string UnknownError = "Unknown Error"; 
        public const string UnsupportedError = "Unsupported Error"; 
        
        public static string MapAccountValidationError(AccountValidationError source) =>
            source switch
            {
                AccountValidationError.None => string.Empty,
                AccountValidationError.AccountDoesNotExist => ValidationErrorCodes.AccountDoesNotExist,
                AccountValidationError.AccountDisabled => ValidationErrorCodes.AccountDisabled,
                _ => UnknownError
            };

        public static string MapInstrumentValidationError(InstrumentValidationError source) =>
            source switch
            {
                InstrumentValidationError.None => string.Empty,
                InstrumentValidationError.InstrumentTradingDisabled => ValidationErrorCodes.InstrumentTradingDisabled,
                InstrumentValidationError.TradesAreNotAvailable => ValidationErrorCodes.TradesAreNotAvailable,
                InstrumentValidationError.NoLiquidity => ValidationErrorCodes.NoLiquidity,
                _ => UnknownError
            };

        public static string MapOrderRejectReason(OrderRejectReason source) =>
            source switch
            {
                OrderRejectReason.InstrumentTradingDisabled => ValidationErrorCodes.InstrumentTradingDisabled,
                OrderRejectReason.NoLiquidity => ValidationErrorCodes.NoLiquidity,
                _ => UnsupportedError
            };
    }
}