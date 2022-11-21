// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using FsCheck;

namespace MarginTradingTests
{
    internal static class Gens
    {
        internal static Gen<int> PositiveLessThan(int max) =>
            Gen.Choose(1, max - 1);
        
        internal static Gen<int> PositiveGreaterThan(int min) =>
            Gen.Choose(min + 1, int.MaxValue);
    }
}