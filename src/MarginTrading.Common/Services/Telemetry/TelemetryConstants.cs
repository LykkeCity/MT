namespace MarginTrading.Common.Services.Telemetry
{
    public static class TelemetryConstants
    {
        #region Event names

        public const string ReadTradingContext = "ReadTradingContext";
        public const string WriteTradingContext = "WriteTradingContext";

        #endregion


        #region Property names

        public const string PendingTimePropName = "PendingTime";
        public const string ProcessingTimePropName = "ProcessingTime";
        public const string ContextDepthPropName = "ContextDepth";

        #endregion
    }
}
