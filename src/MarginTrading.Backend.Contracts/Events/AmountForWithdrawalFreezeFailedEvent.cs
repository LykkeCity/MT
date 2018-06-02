using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Events
{
    public class AmountForWithdrawalFreezeFailedEvent : AccountBalanceMessageBase
    {
        public AmountForWithdrawalFreezeFailedEvent([NotNull] string clientId, [NotNull] string accountId, 
            decimal amount, [NotNull] string operationId, [NotNull] string reason) 
            : base(clientId, accountId, amount, operationId, reason)
        {
        }
    }
}