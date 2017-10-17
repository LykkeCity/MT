using System.Collections.Generic;

namespace MarginTrading.Common.BackendContracts.AccountsManagement
{
    public class CloseAccountPositionsResponse
    {
        public List<CloseAccountPositionsResult> Results { get; set; }
    }

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