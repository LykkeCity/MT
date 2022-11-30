namespace MarginTrading.Backend.Contracts.ErrorCodes
{
    /// <summary>
    /// The list of validation error codes used in <see cref="Refit.ApiException"/>
    /// </summary>
    public static class ValidationErrorCodes
    {
        public const string AccountDoesNotExist = "ACCOUNT_DOES_NOT_EXIST";
        public const string AccountDisabled = "ACCOUNT_DISABLED";
        public const string AccountMismatch = "ACCOUNT_MISMATCH";
        public const string InstrumentTradingDisabled = "INSTRUMENT_TRADING_DISABLED";
        public const string TradesAreNotAvailable = "TRADES_ARE_NOT_AVAILABLE";
        public const string NoLiquidity = "NO_LIQUIDITY";
        public const string PositionNotFound = "POSITION_NOT_FOUND";
    }
}