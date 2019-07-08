// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.Account
{
    /// <summary>
    /// Filter parameters to get accounts with open orders/positions
    /// </summary>
    public class ActiveAccountsRequest
    {
        /// <summary>
        /// List of asset pairs to filter accounts with open orders
        /// </summary>
        /// <remarks>
        /// Null -> no filter will be applied
        /// Empty list -> Account with ANY open order will be returned
        /// </remarks>
        public HashSet<string> ActiveOrderAssetPairIds { get; set; }
        
        /// <summary>
        /// List of asset pairs to filter accounts with open positions
        /// </summary>
        /// <remarks>
        /// Null -> no filter will be applied
        /// Empty list -> Account with ANY open position will be returned
        /// </remarks>
        public HashSet<string> ActivePositionAssetPairIds { get; set; }
        
        /// <summary>
        /// Combine filters by orders and positions with AND clause? (by default OR is applied)
        /// </summary>
        public bool? IsAndClauseApplied { get; set; }
    }
}