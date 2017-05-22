using System;
using MarginTrading.Core;

namespace MarginTrading.Services.Events
{
    public class ElementaryTransactionEventArgs
    {
        public ElementaryTransactionEventArgs(IElementaryTransaction elementaryTransaction)
        {
            if (elementaryTransaction == null) throw new ArgumentNullException(nameof(elementaryTransaction));
            ElementaryTransaction = elementaryTransaction;
        }

        public IElementaryTransaction ElementaryTransaction { get; private set; }
    }
}