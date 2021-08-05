// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests.Unit.SqlConnection
{
    [TestFixture]
    public class PositionExtensionsTests
    {
        private static readonly Random Rnd = new Random();
        
        [TestCase(0)]
        [TestCase(100)]
        public void LargestPnlFirst_Uses_UnrealizedPnl_For_Sorting(int positionsCount)
        {
            var positions = SamplePositions(positionsCount).ToList();

            var actual = positions.AsEnumerable().LargestPnlFirst().ToList();

            var expected = positions.AsEnumerable().OrderByDescending(p => p.GetUnrealisedPnl());
            
            Assert.True(actual.SequenceEqual(expected));
        }

        private static IEnumerable<Position> SamplePositions(int positionCount) =>
            Enumerable.Range(1, positionCount)
                .Select(i => Rnd.Next() / i)
                .Select(pnl =>
                {
                    var m = new Mock<Position>();
                    m.Setup(x => x.GetUnrealisedPnl()).Returns(pnl);
                    return m.Object;
                });
    }
}