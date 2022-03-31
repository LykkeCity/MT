// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Contracts.Common
{
    [JsonConverter(typeof(InitiatorConverter))]
    public sealed class Initiator : IEquatable<Initiator>
    {
        private readonly string _value;

        public Initiator(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));
            
            _value = value;
        }

        public static implicit operator string(Initiator source) => source?._value;

        public static implicit operator Initiator(string source) => new Initiator(source);

        public override string ToString() => _value;

        public bool Equals(Initiator other)
        {
            return _value == other?._value;
        }

        public override bool Equals(object obj)
        {
            return obj is Initiator other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_value != null ? _value.GetHashCode() : 0);
        }
    }
}