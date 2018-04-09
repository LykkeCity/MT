using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.Backend.Contracts.Client;
using MarginTrading.Backend.Contracts.Infrastructure;
using NUnit.Framework;
using Refit;

namespace MarginTradingTests.Backend.Contracts.Client
{
    public class RetryingHttpClientHandlerTests
    {
        [Test]
        public void Always_ShouldRetryCorrectly()
        {
            // arrange
            var refitSettings = new RefitSettings
            {
                HttpMessageHandlerFactory = () =>
                    new RetryingHttpClientHandler(new FakeHttpClientHandler(), 6, new TimeSpan(1))
            };

            var proxy = RestService.For<ITestInterface>("http://fake.host", refitSettings);

            // act
            var invocation = proxy.Invoking(p => p.TestMethod().GetAwaiter().GetResult());

            // assert
            invocation.Should().Throw<ApiException>()
                .WithMessage("Response status code does not indicate success: 502 (Bad Gateway).");
        }
    }

    public interface ITestInterface
    {
        [Get("/fake/url/")]
        Task<string> TestMethod();
    }

    public class FakeHttpClientHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
        }
    }
}