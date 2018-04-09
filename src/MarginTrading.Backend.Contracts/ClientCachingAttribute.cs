using System;

namespace MarginTrading.Backend.Contracts
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientCachingAttribute : Attribute
    {
        private TimeSpan _cachingTime;

        public TimeSpan CachingTime => _cachingTime;

        public int Hours
        {
            set => _cachingTime = new TimeSpan(value, _cachingTime.Minutes, _cachingTime.Seconds);
            get => _cachingTime.Hours;
        }

        public int Minutes
        {
            set => _cachingTime = new TimeSpan(_cachingTime.Hours, value, _cachingTime.Seconds);
            get => _cachingTime.Minutes;
        }

        public int Seconds
        {
            set => _cachingTime = new TimeSpan(_cachingTime.Hours, _cachingTime.Minutes, value);
            get => _cachingTime.Seconds;
        }
    }
}