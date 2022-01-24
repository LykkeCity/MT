// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Exceptions
{
    public class LogInfoOnlyException : Exception
    {
        public LogInfoOnlyException(string message, Exception innerException):base(message, innerException)
        { 
        }
    }
}