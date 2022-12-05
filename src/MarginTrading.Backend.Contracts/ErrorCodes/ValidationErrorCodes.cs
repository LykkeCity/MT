namespace MarginTrading.Backend.Contracts.ErrorCodes
{
    /// <summary>
    /// The list of validation error codes used in <see cref="Refit.ApiException"/>
    /// </summary>
    public static class ValidationErrorCodes
    {
        #region General
        /// <summary>
        /// The trades are off
        /// </summary>
        public const string TradesAreNotAvailable = "TRADES_ARE_NOT_AVAILABLE";
        #endregion
        
        #region Account 
        /// <summary>
        /// The account is not found
        /// </summary>
        public const string AccountDoesNotExist = "ACCOUNT_DOES_NOT_EXIST";
        
        /// <summary>
        /// The account is not active
        /// </summary>
        public const string AccountDisabled = "ACCOUNT_DISABLED";
        
        /// <summary>
        /// Entity ownership validation error
        /// </summary>
        public const string AccountMismatch = "ACCOUNT_MISMATCH";
        
        /// <summary>
        /// The account was not provided but required
        /// </summary>
        public const string AccountEmpty = "ACCOUNT_EMPTY";
        #endregion
        
        #region Asset
        /// <summary>
        /// The instrument trading is not allowed
        /// </summary>
        public const string InstrumentTradingDisabled = "INSTRUMENT_TRADING_DISABLED";

        /// <summary>
        /// There is no liquidity for the instrument
        /// </summary>
        public const string InstrumentNoLiquidity = "NO_LIQUIDITY";
        #endregion
        
        #region Position
        /// <summary>
        /// Position is not found
        /// </summary>
        public const string PositionNotFound = "POSITION_NOT_FOUND";
        
        /// <summary>
        /// Position is in invalid state when trying to run special liquidation
        /// </summary>
        public const string PositionInvalidStatusSpecialLiquidation = "POSITION_INVALID_STATUS_SPECIAL_LIQUIDATION";
        #endregion
        
        #region Position group
        /// <summary>
        /// Position group direction was not provided when required
        /// </summary>
        public const string PositionGroupDirectionEmpty = "POSITION_GROUP_DIRECTION_EMPTY";
        
        /// <summary>
        /// Position group has positions with different accounts 
        /// </summary>
        public const string PositionGroupMultipleAccounts = "POSITION_GROUP_MULTIPLE_ACCOUNTS";
        
        /// <summary>
        /// Position group has positions with different instruments
        /// </summary>
        public const string PositionGroupMultipleInstruments = "POSITION_GROUP_MULTIPLE_INSTRUMENTS";
        
        /// <summary>
        /// Position group has positions with both directions
        /// </summary>
        public const string PositionGroupMultipleDirections = "POSITION_GROUP_MULTIPLE_DIRECTIONS";
        #endregion
        
        #region Order
        /// <summary>
        /// Order is not found
        /// </summary>
        public const string OrderNotFound = "ORDER_NOT_FOUND";
        
        /// <summary>
        /// Order is in invalid state when trying to cancel it
        /// </summary>
        public const string OrderIncorrectStatus = "ORDER_INCORRECT_STATUS";
        #endregion
    }
}