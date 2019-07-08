// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class CloseAccountPositionsResult
    {
        public string AccountId { get; set; }
        
        /// <summary>
        /// List of closed positions
        /// </summary>
        public OrderFullContract[] ClosedPositions { get; set; }
        
        /// <summary>
        /// Error message, if positions were not closed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}