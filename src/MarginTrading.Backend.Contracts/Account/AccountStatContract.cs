namespace MarginTrading.Backend.Contracts.Account
{
    public class AccountStatContract
    {
        /// <summary>
        /// ID
        /// </summary>
        public string AccountId { get; set; }
        
        /// <summary>
        /// Base asset ID
        /// </summary>
        public string BaseAssetId { get; set; }
        
        /// <summary>
        /// Sum of all cash movements except for unrealized PnL 
        /// </summary>
        public decimal Balance { get; set; }
        
        /// <summary>
        /// Margin call level
        /// </summary>
        public decimal MarginCallLevel { get; set; }
        
        /// <summary>
        /// Stop out level
        /// </summary>
        public decimal StopOutLevel { get; set; }
        
        /// <summary>
        /// Balance + UnrealizedPnL
        /// </summary>
        public decimal TotalCapital { get; set; }
        
        /// <summary>
        /// TotalCapital - UsedMargin
        /// </summary>
        public decimal FreeMargin { get; set; }
        
        /// <summary>
        /// TotalCapital - MarginInit
        /// </summary>
        public decimal MarginAvailable { get; set; }
        
        /// <summary>
        /// Margin used for maintenance of positions (considering MCO rule)
        /// = Max (CurrentlyUsedMargin, InitiallyUsedMargin/2)
        /// </summary>
        public decimal UsedMargin { get; set; }
        
        /// <summary>
        /// Margin used by open positions
        /// </summary>
        public decimal CurrentlyUsedMargin { get; set; }
        
        /// <summary>
        /// Margin used for initial open of existing positions
        /// </summary>
        public decimal InitiallyUsedMargin { get; set; }
        
        /// <summary>
        /// Margin used for calculations, when open new positions
        /// </summary>
        public decimal MarginInit { get; set; }
        
        /// <summary>
        /// Unrealized PnL
        /// </summary>
        public decimal PnL { get; set; }
        
        /// <summary>
        /// Unrealized daily PnL
        /// </summary>
        public decimal UnrealizedDailyPnl { get; set; }
        
        /// <summary>
        /// Number of opened positions
        /// </summary>
        public int OpenPositionsCount { get; set; }
        
        /// <summary>
        /// Number of active orders
        /// </summary>
        public int ActiveOrdersCount { get; set; }
        
        /// <summary>
        /// TotalCapital / UsedMargin
        /// </summary>
        public decimal MarginUsageLevel { get; set; }
        
        /// <summary>
        /// Legal Entity of account
        /// </summary>
        public string LegalEntity { get; set; }
        
        /// <summary>
        /// If account is in liquidation state
        /// </summary>
        public bool IsInLiquidation { get; set; }

        /// <summary>
        /// Margin warning notification level
        /// </summary>
        public string MarginNotificationLevel { get; set; }
    }
}