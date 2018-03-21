using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.Backend.Core
{
    public interface IDayOffSettingsRepository
    {
        [CanBeNull] DayOffSettingsRoot Read();
        void Write(DayOffSettingsRoot settings);
    }
}