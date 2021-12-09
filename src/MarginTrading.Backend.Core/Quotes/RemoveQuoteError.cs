// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Quotes
{
    public class RemoveQuoteError
    {
        private RemoveQuoteError(string message, RemoveQuoteErrorCode errorCode)
        {
            if (string.IsNullOrWhiteSpace(message) && errorCode != RemoveQuoteErrorCode.None)
                throw new ArgumentException("Message can't be empty", nameof(message));
            
            Message = message;
            ErrorCode = errorCode;
        }

        public string Message { get; }
        
        public RemoveQuoteErrorCode ErrorCode { get; }
        
        public static implicit operator RemoveQuoteErrorCode(RemoveQuoteError e) => e.ErrorCode;

        public static RemoveQuoteError Success() => new RemoveQuoteError(string.Empty, RemoveQuoteErrorCode.None);

        public static RemoveQuoteError Failure(string message, RemoveQuoteErrorCode errorCode) => new RemoveQuoteError(message, errorCode);
    }
}