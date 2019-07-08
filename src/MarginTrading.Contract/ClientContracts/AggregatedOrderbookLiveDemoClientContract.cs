// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class AggregatedOrderbookLiveDemoClientContract
    {
        public AggregatedOrderbookClientContract Live { get; set; }
        public AggregatedOrderbookClientContract Demo { get; set; }
    }
}