// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public sealed class AccountValidationException : ValidationException<AccountValidationError>
    {
        public AccountValidationException(AccountValidationError errorCode) : base(errorCode)
        {
        }

        public AccountValidationException(string message, AccountValidationError errorCode) : base(message, errorCode)
        {
        }

        public AccountValidationException(string message, AccountValidationError errorCode, Exception innerException) :
            base(message, errorCode, innerException)
        {
        }
    }
}