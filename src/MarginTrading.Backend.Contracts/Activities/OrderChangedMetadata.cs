// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Activities
{
    public class OrderChangedMetadata
    {
        public OrderChangedProperty UpdatedProperty { get; set; }
        
        public string OldValue { get; set; }
    }
}