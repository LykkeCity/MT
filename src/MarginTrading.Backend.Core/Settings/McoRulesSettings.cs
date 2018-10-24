using JetBrains.Annotations;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class McoRulesSettings
    {
        public McoLevelSettings LongMcoLevels { get; set; }
        public McoLevelSettings ShortMcoLevels { get; set; }
    }
}