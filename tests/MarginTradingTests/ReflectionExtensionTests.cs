// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MarginTrading.Common.Extensions;
using NUnit.Framework;

namespace MarginTradingTests
{
    public class ReflectionExtensionTests
    {
        class SampleClass
        {
            public interface INestedInterface
            {
                
            }
            public class NestedClass : INestedInterface
            {
            }
            public struct NestedStruct
            {
            }
            
            public NestedClass Class { get; set; }
            public NestedStruct Struct { get; set; }

            public SampleClass()
            {
                Class = new NestedClass();
                Struct = new NestedStruct();
            }
        }

        [Test]
        public void GetPropertiesOfType_ReturnsClasses()
        {
            var sut = new SampleClass();
            
            var result = sut.GetPropertiesOfType<SampleClass.NestedClass>();
            
            Assert.AreEqual(1, result.Count());
        }
        
        [Test]
        public void GetPropertiesOfType_ReturnsInterfaces()
        {
            var sut = new SampleClass();
            
            var result = sut.GetPropertiesOfType<SampleClass.INestedInterface>();
            
            Assert.AreEqual(1, result.Count());
        }

        [Test]
        public void GetPropertiesOfType_ReturnsStructs()
        {
            var sut = new SampleClass();
            
            var result = sut.GetPropertiesOfType<SampleClass.NestedStruct>();
            
            Assert.AreEqual(1, result.Count());
        }
    }
}