// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTradingTests.Helpers;
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
            var positions = SamplePositionsWithPnl(positionsCount).ToList();

            var actual = positions.AsEnumerable().LargestPnlFirst().ToList();

            var expected = positions.AsEnumerable().OrderByDescending(p => p.GetUnrealisedPnl());
            
            Assert.True(actual.SequenceEqual(expected));
        }
        
        [Test]
        public void SummarizeVolume_EmptyPositionsList_GivesZeroValues()
        {
            var positions = Enumerable.Empty<Position>();

            var result = positions.SummarizeVolume();
            
            Assert.AreEqual(0, result.Margin);
            Assert.AreEqual(0, result.Volume);
        }

        [Test]
        public void SummarizeVolume_DifferentDirectionPositions_ThrowsException()
        {
            var positions = new List<Position>
            {
                DumbDataGenerator.GeneratePosition(volume: 1),
                DumbDataGenerator.GeneratePosition(volume: -1)
            };

            Assert.Throws<InvalidOperationException>(() => positions.SummarizeVolume());
        }

        [Test]
        public void SummarizeVolume_Calculates_Summary_Of_Absolute_Values()
        {
            const int start = 1;
            const int count = 10;
            var randomSign = Rnd.Next(2) * 2 - 1;
            
            var result = Enumerable
                .Range(start, count)
                .Select(i => randomSign * i)
                .Select(i => DumbDataGenerator.GeneratePosition(volume: i))
                .SummarizeVolume();
            
            var expected = count * (count + 1) / 2; // arithmetic progression summary
            
            Assert.AreEqual(expected, result.Volume);
            Assert.True(result.Volume > 0);
        }

        [Test]
        public void SummarizeVolume_Calculates_MarginMaintenance_Summary()
        {
            const int start = 1;
            const int count = 10;

            var result = Enumerable
                .Range(start, count)
                .Select(m => DumbDataGenerator.GeneratePosition(margin: m))
                .SummarizeVolume();
            
            var expected = count * (count + 1) / 2; // arithmetic progression summary
            
            Assert.AreEqual(expected, result.Margin);
        }

        private static IEnumerable<Position> SamplePositionsWithPnl(int positionCount) =>
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