// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISnapshotService
    {
        Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId);
    }
}