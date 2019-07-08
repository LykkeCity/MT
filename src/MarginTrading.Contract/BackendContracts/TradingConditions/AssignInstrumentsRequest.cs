// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts.TradingConditions
{
    public class AssignInstrumentsRequest
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string[] Instruments { get; set; }
    }
}