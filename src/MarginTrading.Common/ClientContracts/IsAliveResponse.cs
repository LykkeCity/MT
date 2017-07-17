namespace MarginTrading.Common.ClientContracts
{
    public class IsAliveResponse
    {
        public string Version { get; set; }
        public string Env { get; set; }
    }

    public class IsAliveExtendedResponse : IsAliveResponse
    {
        public string DemoVersion { get; set; }
        public string LiveVersion { get; set; }
        public int WampOpened { get; set; }
    }
}
