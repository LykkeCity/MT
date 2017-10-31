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
