// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace MarginTrading.Backend.Services
{
    public static class AlphanumericIdentityGenerator
    {
        private const string Pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly Random Random = new Random();
        private static readonly object LockObject = new object();
        
        public static string GenerateAlphanumericId()
        {
            lock(LockObject)
            {
                var chars = Enumerable.Range(0, 10).Select(x => Pool[Random.Next(0, Pool.Length)]);
                return new string(chars.ToArray());
            }
        }
    }
}