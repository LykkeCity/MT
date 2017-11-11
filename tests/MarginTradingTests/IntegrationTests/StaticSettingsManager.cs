using System.Threading.Tasks;
using Lykke.SettingsReader;

namespace MarginTradingTests.IntegrationTests
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

    public static class StringExtensions
    {
        public static StaticSettingsManager<string> MakeSettings(this string str)
        {
            return new StaticSettingsManager<string>(str);
        }
    }
}
