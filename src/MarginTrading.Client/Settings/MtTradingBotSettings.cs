namespace MarginTrading.Client.Settings
{
    class MtTradingBotSettings
    {
        public string MTServerAddress { get; set; }
        public string MTAuthorizationAddress { get; set; }
        public int NumberOfUsers { get; set; }
        public int ActionScriptInterval { get; set; }
        public int TransactionFrequencyMin { get; set; }
        public int TransactionFrequencyMax { get; set; }
        public string UsersFile { get; set; } = null;
        public string TestFile { get; set; } = null;
    }
}
