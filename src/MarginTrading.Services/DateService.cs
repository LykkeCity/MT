using System;
using MarginTrading.Backend.Core;

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
