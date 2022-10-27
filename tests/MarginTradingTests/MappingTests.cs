// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Services;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MappingTests
    {
        [Test]
        public void ShouldHaveValidMappingConfiguration()
        {
            var convertService = new ConvertService();
            
            convertService.AssertConfigurationIsValid();
        }
    }
}