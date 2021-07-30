// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;

namespace MarginTrading.Backend.Contracts.Testing
{
    public class AddOrUpdateFakeSnapshotRequest
    {
        public DateTime TradingDay { get; set; }

        public string CorrelationId { get; set; }

        public List<OrderContract> Orders { get; set; } = new List<OrderContract>();

        public List<OpenPositionContract> Positions { get; set; } = new List<OpenPositionContract>();

        public List<AccountStatContract> Accounts { get; set; } = new List<AccountStatContract>();

        public Dictionary<string, BestPriceContract> BestFxPrices { get; set; } =
            new Dictionary<string, BestPriceContract>();

        public Dictionary<string, BestPriceContract> BestTradingPrices { get; set; } =
            new Dictionary<string, BestPriceContract>();
    }
}