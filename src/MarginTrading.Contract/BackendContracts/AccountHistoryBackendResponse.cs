// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public class AccountHistoryBackendResponse
    {
        public AccountHistoryBackendContract[] Account { get; set; }
        public OrderHistoryBackendContract[] PositionsHistory { get; set; }
        public OrderHistoryBackendContract[] OpenPositions { get; set; }
    }
}
