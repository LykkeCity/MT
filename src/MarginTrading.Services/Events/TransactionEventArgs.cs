using System;
using MarginTrading.Core;

namespace MarginTrading.Services.Events
{
    public class TransactionEventArgs
    {
        public TransactionEventArgs(ITransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            Transaction = transaction;
        }

        public ITransaction Transaction { get; private set; }
    }
}