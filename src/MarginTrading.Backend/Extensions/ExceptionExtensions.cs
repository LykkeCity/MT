// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Exceptions;

namespace MarginTrading.Backend.Extensions
{
    internal static class ExceptionExtensions
    {
        public static bool IsValidationException(this Exception exception)
        {
            var baseType = exception.GetType().BaseType;
            
            if (baseType == null)
                return false;
            
            return baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ValidationException<>);
        }

        [CanBeNull]
        public static Type GetValidationErrorType(this Exception exception)
        {
            var baseType = exception.GetType().BaseType;
            
            if (baseType == null)
                return null;

            return baseType.GetGenericArguments()[0];
        }

        [CanBeNull]
        public static object GetValidationErrorValue(this Exception exception) =>
            exception
                .GetType()
                .GetProperty("ErrorCode")?
                .GetValue(exception);
    }
}