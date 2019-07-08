// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Common.Services
{
    public class DateService : IDateService
    {
        public DateTime Now()
        {
            return DateTime.UtcNow;
        }
    }
}
