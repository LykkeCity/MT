// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Snapshots
{
    /// <summary>
    /// Represents short information of a position that used for state validation.
    /// </summary>
    public class PositionInfo
    {
        public PositionInfo()
        {
        }

        public PositionInfo(string id, decimal volume)
        {
            Id = id;
            Volume = volume;
        }

        public string Id { get; set; }

        public decimal Volume { get; set; }
    }
}