// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class TradingSnapshotDraftNotFoundException : Exception
    {
        public TradingSnapshotDraftNotFoundException(DateTime tradingDay): base($"Couldn't find trading snapshot draft for date [{tradingDay}]")
        {
        }
    }
}