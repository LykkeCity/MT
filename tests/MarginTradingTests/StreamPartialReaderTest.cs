// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using MarginTrading.Common.Helpers;
using NUnit.Framework;
using Random = System.Random;

namespace MarginTradingTests
{
    [TestFixture]
    internal class StreamPartialReaderTest
    {
        [Test]
        public async Task ReadBytes_When_Original_Stream_Empty_Returns_Empty()
        {
            byte[] buffer = CreateBuffer(0);
            using var stream = new MemoryStream(buffer);
            
            var result = await stream.ReadBytes(10);
            
            Assert.AreEqual(0, result.Length);
        }

        public async Task ReadBytes_When_Read_Size_Invalid_Returns_Empty()
        {
            byte[] buffer = CreateBuffer(100);
            using var stream = new MemoryStream(buffer);
            
            var result = await stream.ReadBytes(0);
            
            Assert.AreEqual(0, result.Length);
        }

        [FsCheck.NUnit.Property]
        public Property ReadBytes_When_Requested_More_Than_Buffer_Returns_Buffer()
        {
            return Prop.ForAll(
                (from bs in Arb.Default.PositiveInt().Generator
                    from rs in Gens.PositiveGreaterThan(bs.Get)
                    select (bufferSize: bs.Item, requestedSize: rs)).ToArbitrary(), t =>
                {
                    byte[] buffer = CreateBuffer(t.bufferSize);
                    using var stream = new MemoryStream(buffer);
            
                    var result = stream.ReadBytes((uint)t.requestedSize).GetAwaiter().GetResult();
            
                    Assert.AreEqual(t.bufferSize, result.Length);
                });
        }
        
        [FsCheck.NUnit.Property]
        public Property ReadBytes_HappyPath()
        {
            return Prop.ForAll(
                (from bs in Arb.Default.PositiveInt().Generator
                    from rs in Gens.PositiveLessThan(bs.Get)
                    select (bufferSize: bs.Item, requestedSize: rs)).ToArbitrary(), t => 
                {
                    byte[] buffer = CreateBuffer(t.bufferSize);
                    using var stream = new MemoryStream(buffer);
                    
                    var result = stream.ReadBytes((uint)t.requestedSize).GetAwaiter().GetResult();
                    
                    Assert.AreEqual(t.requestedSize, result.Length);
                    Assert.True(result.SequenceEqual(buffer.Take(t.requestedSize)));
                });
        }
        
        
        private static byte[] CreateBuffer(int size)
        {
            var buffer = new byte[size];
            new Random().NextBytes(buffer);
            return buffer;
        }
    }
}