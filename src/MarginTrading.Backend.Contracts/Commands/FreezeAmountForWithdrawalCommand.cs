namespace MarginTrading.Backend.Contracts.Commands
{
    public class FreezeAmountForWithdrawalCommand : AccountBalanceMessageBase
    {
        public FreezeAmountForWithdrawalCommand(string clientId, string accountId, decimal amount, string operationId,
            string reason) : base(clientId, accountId, amount, operationId, reason)
        {
        }
    }
}