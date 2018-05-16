namespace MarginTrading.Backend.Contracts.Events
{
    public class AmountForWithdrawalFrozenEvent : AccountBalanceMessageBase
    {
        public AmountForWithdrawalFrozenEvent(string clientId, string accountId, decimal amount, string operationId,
            string reason) : base(clientId, accountId, amount, operationId, reason)
        {
        }
    }
}