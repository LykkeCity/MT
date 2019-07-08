// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountHistoryBackendResponse
    {
        public AccountHistoryBackendContract[] Account { get; set; }
        public OrderHistoryBackendContract[] PositionsHistory { get; set; }
        public OrderHistoryBackendContract[] OpenPositions { get; set; }
    }
}
