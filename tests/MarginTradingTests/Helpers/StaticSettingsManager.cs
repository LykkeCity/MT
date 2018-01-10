using System.Threading.Tasks;
using Lykke.SettingsReader;

namespace MarginTradingTests.Helpers
{
    public class StaticSettingsManager<T> : IReloadingManager<T>
    {
        public StaticSettingsManager(T currentValue)
        {
            HasLoaded = true;
            CurrentValue = currentValue;
        }

        public Task<T> Reload()
        {
            return Task.FromResult(CurrentValue);
        }

        public bool HasLoaded { get; }
        public T CurrentValue { get; }
    }
}