using System;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class DateService : IDateService
    {
        public DateTime Now()
        {
            return DateTime.UtcNow;
        }
    }
}
