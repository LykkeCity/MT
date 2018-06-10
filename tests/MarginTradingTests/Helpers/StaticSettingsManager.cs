using System;
using System.Threading.Tasks;
using Lykke.SettingsReader;

namespace MarginTradingTests.Helpers
{
    public class StaticSettingsManager<T> : IReloadingManager<T>
    {
        private DateTime _lastReload;

        public StaticSettingsManager(T currentValue)
        {
            HasLoaded = true;
            CurrentValue = currentValue;
        }

        public Task<T> Reload()
        {
            _lastReload = DateTime.Now;
            return Task.FromResult(CurrentValue);
        }

        public bool WasReloadedFrom(DateTime dateTime) => (dateTime < _lastReload);
        public bool HasLoaded { get; }
        public T CurrentValue { get; }
    }
}