// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.Orders
{
    /// <summary>
    /// The direction of an order
    /// </summary>
    public enum OrderDirectionContract
    {
        /// <summary>
        /// Order to buy the quoting asset of a pair
        /// </summary>
        Buy = 1,
        
        /// <summary>
        /// Order to sell the quoting asset of a pair
        /// </summary>
        Sell = 2
    }}