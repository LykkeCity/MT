// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Extensions;
using MarginTrading.Backend.Middleware;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    internal class ValidationExceptionHandlerTests
    {
        [Test]
        public void CanHandle_WhenExceptionIsAccountValidationException_ReturnsTrue()
        {
            var ex = new AccountValidationException(AccountValidationError.None);

            ValidationExceptionHandler.CanHandleException(ex).Should().Be(true);
        }
        
        [Test]
        public void CanHandle_WhenExceptionIsInstrumentValidationException_ReturnsTrue()
        {
            var ex = new InstrumentValidationException(InstrumentValidationError.None);

            ValidationExceptionHandler.CanHandleException(ex).Should().Be(true);
        }
        
        [Test]
        public void CanHandle_WhenExceptionIsOrderValidationException_And_PublicErrorCodeAvailable_ReturnsTrue(
            [Random(100)] OrderRejectReason rejectReason)
        {
            var ex = new OrderRejectionException(rejectReason, "message");

            ValidationExceptionHandler.CanHandleException(ex).Should().Be(ex.IsPublic());
        }
    }
}