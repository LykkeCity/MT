// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core
{
    public interface IAccountMarginFreezing
    {
        string OperationId { get; }
        string AccountId { get; }
        decimal Amount { get; }
    }
}