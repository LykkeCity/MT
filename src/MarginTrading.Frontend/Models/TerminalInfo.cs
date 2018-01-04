namespace MarginTrading.Frontend.Models
{
    public class TerminalInfo
    {
        public string Name { get; }
        public bool DemoEnabled { get; }
        public bool LiveEnabled { get; }

        public TerminalInfo(string name, bool demoEnabled, bool liveEnabled)
        {
            Name = name;
            DemoEnabled = demoEnabled;
            LiveEnabled = liveEnabled;
        }
    }
}