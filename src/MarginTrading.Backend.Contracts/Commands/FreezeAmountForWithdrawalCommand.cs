using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Commands
{
    public class FreezeAmountForWithdrawalCommand : AccountBalanceMessageBase
    {
        public FreezeAmountForWithdrawalCommand([NotNull] string clientId, [NotNull] string accountId, decimal amount, 
            [NotNull] string operationId, [NotNull] string reason) 
            : base(clientId, accountId, amount, operationId, reason)
        {
        }
    }
}