// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Exceptions;

namespace MarginTrading.Backend.Extensions
{
    internal static class ValidateOrderExceptionExtensions
    {
        public static bool IsPublic(this OrderRejectionException ex)
        {
            var publicErrorCode = PublicErrorCodeMap.Map(ex.RejectReason);
            return publicErrorCode != PublicErrorCodeMap.UnsupportedError;
        }
    }
}