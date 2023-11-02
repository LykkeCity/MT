// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Snapshots;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Extensions
{
    internal static class SnapshotExtensions
    {
        public static SnapshotStatus? ToDomain(this SnapshotStatusContract src)
        {
            var mapped = Enum.TryParse<SnapshotStatus>(src.ToString(), true, out var result);
            return mapped ? result : (SnapshotStatus?)null;
        }
    }
}