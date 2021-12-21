// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.SqlRepositories.Entities
{
    /// <summary>
    /// Snapshot status as a string for using as a part of DB entity 
    /// </summary>
    public class SnapshotStatusString
    {
        private readonly string _value;

        private SnapshotStatusString(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentOutOfRangeException(nameof(status));
            
            _value = status;
        }

        public SnapshotStatusString(SnapshotStatus status)
        {
            _value = status.ToString();
        }

        public static implicit operator string(SnapshotStatusString src) => src?._value ?? string.Empty;

        public static implicit operator SnapshotStatusString(string src) => new SnapshotStatusString(src);

        public static implicit operator SnapshotStatus(SnapshotStatusString src)
        {
            if (!Enum.TryParse<SnapshotStatus>(src, true, out var result))
                throw new ArgumentOutOfRangeException(nameof(src),
                    $"Unexpected value of snapshot status string ({src._value})");

            return result;
        }

        public static implicit operator SnapshotStatusString(SnapshotStatus src) => new SnapshotStatusString(src);

        public override string ToString()
        {
            return _value;
        }
    }
}