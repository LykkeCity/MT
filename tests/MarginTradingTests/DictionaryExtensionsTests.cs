// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Extensions;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class DictionaryExtensionsTests
    {
        [Test]
        public void Merge_Source_IsNullOrEmpty_Returns_Same_Collection()
        {
            var englishNumbers = new Dictionary<string, string>{{"1", "one"}, {"2", "two"}};

            var result = englishNumbers.Merge(null);
            
            Assert.That(result.SequenceEqual(englishNumbers));
        }

        [Test]
        public void Merge_Same_Keys_Replaced()
        {
            var englishNumbers = new Dictionary<string, string>{{"1", "one"}, {"2", "two"}};

            var spanishNumbers = new Dictionary<string, string> { { "2", "dos" } };

            englishNumbers.Merge(spanishNumbers);

            Assert.Multiple(() =>
            {
                Assert.That(englishNumbers, Does.ContainKey("1").With.ContainValue("one"));
                Assert.That(englishNumbers, Does.ContainKey("2").With.ContainValue("dos"));
                Assert.That(englishNumbers, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void Merge_Unique_Keys_Added()
        {
            var englishNumbers = new Dictionary<string, string>{{"1", "one"}, {"2", "two"}};

            var spanishNumbers = new Dictionary<string, string> { { "3", "tres" } };

            var mixed = englishNumbers.Merge(spanishNumbers);
            
            Assert.Multiple(() =>
            {
                Assert.That(mixed, Has.Count.EqualTo(3));
                Assert.That(mixed, Does.ContainKey("1").With.ContainValue("one"));
                Assert.That(mixed, Does.ContainKey("2").With.ContainValue("two"));
                Assert.That(mixed, Does.ContainKey("3").With.ContainValue("tres"));
            });
        }
    }
}