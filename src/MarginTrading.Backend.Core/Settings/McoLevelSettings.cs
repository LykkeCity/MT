using JetBrains.Annotations;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class McoLevelSettings
    {
        public decimal MarginCall1 { get; set; }
        public decimal MarginCall2 { get; set; }
        public decimal StopOut { get; set; }
    }
}