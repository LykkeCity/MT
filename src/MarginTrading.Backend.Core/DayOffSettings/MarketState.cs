// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class MarketState
    {
        public string Id { get; set; }

        public bool IsEnabled { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, IsEnabled: {IsEnabled}.";
        }
    }
}