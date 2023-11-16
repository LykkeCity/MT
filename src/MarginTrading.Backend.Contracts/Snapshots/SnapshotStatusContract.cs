// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Snapshots
{
    [PublicAPI]
    public enum SnapshotStatusContract
    {
        Draft = 0,
        Final
    }
}