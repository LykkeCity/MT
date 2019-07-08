// Copyright (c) 2019 Lykke Corp.

using System;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISnapshotService
    {
        Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId);
    }
}