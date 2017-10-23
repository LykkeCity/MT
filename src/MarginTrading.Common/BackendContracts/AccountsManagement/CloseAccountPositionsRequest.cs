namespace MarginTrading.Common.BackendContracts.AccountsManagement
{
    public class CloseAccountPositionsRequest
    {
        /// <summary>
        /// List of account ids
        /// </summary>
        public string[] AccountIds { get; set; }
        
        /// <summary>
        /// If "false", orders are closed only if account is in margin call state
        /// If "true", orders are closed anyway
        /// </summary>
        public bool IgnoreMarginLevel { get; set; }
    }
}