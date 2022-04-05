// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using Dapper;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.SqlRepositories.Entities
{
    /// <summary>
    /// Snapshot status as a string for using as a part of DB entity 
    /// </summary>
    public class SnapshotStatusString : IConvertible
    {
        private readonly string _value;
        
        static SnapshotStatusString()
        {
            SqlMapper.AddTypeMap(typeof(SnapshotStatusString), DbType.String);
        }

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

        #region IConvertible implementation
        
        public TypeCode GetTypeCode() => TypeCode.Object;

        public bool ToBoolean(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider? provider)
        {
            return _value;
        }

        public object ToType(Type conversionType, IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}