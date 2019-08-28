// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core
{
    public class WithdrawalFreezeOperationData : OperationDataBase<OperationState>
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}