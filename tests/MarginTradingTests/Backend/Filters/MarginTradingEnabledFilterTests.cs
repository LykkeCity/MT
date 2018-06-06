using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using FluentAssertions;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using Rocks.Caching;

namespace MarginTradingTests.Backend.Filters
{
    [TestFixture]
    public class MarginTradingEnabledFilterTests
    {
        [Test]
        public void ActionWithoutAccountId_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService =
                Mock.Of<IMarginTradingSettingsCacheService>(s =>
                    s.IsMarginTradingEnabledByAccountId("id of account") == false);
            var sut = new MarginTradingEnabledFilter(marginTradingSettingsService, new DummyCacheProvider(),
                new Mock<ILog>().Object);

            //act
            var context = new ActionExecutingContext(new ControllerContext
            {
                ActionDescriptor = new ControllerActionDescriptor
                {
                    DisplayName = "action display name",
                    Parameters =
                        new List<ParameterDescriptor>
                        {
                            new ControllerParameterDescriptor {Name = "i", ParameterType = typeof(int),}
                        },
                    MethodInfo = typeof(TestController).GetMethod("ActionWithoutAccountId"),
                },
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            }, new List<IFilterMetadata>(), new Dictionary<string, object>(), new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.Should().NotThrow();
        }


        [Test]
        public void ActionWithAccountIdParam_IfTradingEnabled_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService =
                Mock.Of<IMarginTradingSettingsCacheService>(s =>
                    s.IsMarginTradingEnabledByAccountId("id of account") == true);
            var sut = new MarginTradingEnabledFilter(marginTradingSettingsService, new DummyCacheProvider(),
                new Mock<ILog>().Object);

            //act
            var context = new ActionExecutingContext(new ControllerContext
                {
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        DisplayName = "action display name",
                        Parameters =
                            new List<ParameterDescriptor>
                            {
                                new ControllerParameterDescriptor {Name = "accountId", ParameterType = typeof(string),}
                            },
                        MethodInfo = typeof(TestController).GetMethod("ActionWithAccountIdParam"),
                    },
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                }, new List<IFilterMetadata>(), new Dictionary<string, object> {{"accountId", "id of account"}},
                new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.Should().NotThrow();
        }


        [Test]
        public void ActionWithAccountIdParam_IfTradingDisabled_ShouldThrow()
        {
            //arrange
            var marginTradingSettingsService =
                Mock.Of<IMarginTradingSettingsCacheService>(s =>
                    s.IsMarginTradingEnabledByAccountId("id of account") == false);
            var sut = new MarginTradingEnabledFilter(marginTradingSettingsService, new DummyCacheProvider(),
                new Mock<ILog>().Object);

            //act
            var context = new ActionExecutingContext(new ControllerContext
                {
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        DisplayName = "action display name",
                        Parameters =
                            new List<ParameterDescriptor>
                            {
                                new ControllerParameterDescriptor {Name = "accountId", ParameterType = typeof(string),}
                            },
                        MethodInfo = typeof(TestController).GetMethod("ActionWithAccountIdParam"),
                    },
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                }, new List<IFilterMetadata>(), new Dictionary<string, object> {{"accountId", "id of account"}},
                new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.Should().Throw<InvalidOperationException>()
                .WithMessage("Using this type of margin trading is restricted for account id id of account");
        }


        [Test]
        public void ActionWithRequestParam_IfTradingEnabled_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService =
                Mock.Of<IMarginTradingSettingsCacheService>(s =>
                    s.IsMarginTradingEnabledByAccountId("id of account") == true);
            var sut = new MarginTradingEnabledFilter(marginTradingSettingsService, new DummyCacheProvider(),
                new Mock<ILog>().Object);

            //act
            var context = new ActionExecutingContext(new ControllerContext
                {
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        DisplayName = "action display name",
                        Parameters =
                            new List<ParameterDescriptor>
                            {
                                new ControllerParameterDescriptor
                                {
                                    Name = "request",
                                    ParameterType = typeof(RequestWithAccountId),
                                }
                            },
                        MethodInfo = typeof(TestController).GetMethod("ActionWithRequestParam"),
                    },
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                }, new List<IFilterMetadata>(),
                new Dictionary<string, object> {{"request", new RequestWithAccountId {AccountId = "id of account"}}},
                new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.Should().NotThrow();
        }


        [Test]
        public void ActionWithRequestParam_IfTradingDisabled_ShouldThrow()
        {
            //arrange
            var marginTradingSettingsService =
                Mock.Of<IMarginTradingSettingsCacheService>(s =>
                    s.IsMarginTradingEnabledByAccountId("id of account") == false);
            var sut = new MarginTradingEnabledFilter(marginTradingSettingsService, new DummyCacheProvider(),
                new Mock<ILog>().Object);

            //act
            var context = new ActionExecutingContext(new ControllerContext
                {
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        DisplayName = "action display name",
                        Parameters =
                            new List<ParameterDescriptor>
                            {
                                new ControllerParameterDescriptor
                                {
                                    Name = "request",
                                    ParameterType = typeof(RequestWithAccountId),
                                }
                            },
                        MethodInfo = typeof(TestController).GetMethod("ActionWithRequestParam"),
                    },
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                }, new List<IFilterMetadata>(),
                new Dictionary<string, object> {{"request", new RequestWithAccountId {AccountId = "id of account"}}},
                new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.Should().Throw<InvalidOperationException>()
                .WithMessage("Using this type of margin trading is restricted for account id id of account");
        }


        [Test]
        public void ActionWithSkipAttribute_IfTradingDisabled_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService =
                Mock.Of<IMarginTradingSettingsCacheService>(s =>
                    s.IsMarginTradingEnabledByAccountId("id of account") == false);
            var sut = new MarginTradingEnabledFilter(marginTradingSettingsService, new DummyCacheProvider(),
                new Mock<ILog>().Object);

            //act
            var context = new ActionExecutingContext(new ControllerContext
                {
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        DisplayName = "action display name",
                        Parameters =
                            new List<ParameterDescriptor>
                            {
                                new ControllerParameterDescriptor {Name = "accountId", ParameterType = typeof(string),}
                            },
                        MethodInfo = typeof(TestController).GetMethod("ActionWithSkipAttribute"),
                    },
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                }, new List<IFilterMetadata>(), new Dictionary<string, object> {{"accountId", "id of account"}},
                new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.Should().NotThrow();
        }


        private static Task<ActionExecutedContext> NextFunc()
        {
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()),
                new List<IFilterMetadata>(), new TestController()));
        }

        private class TestController
        {
            public bool ActionWithoutAccountId(int i) => true;
            public bool ActionWithAccountIdParam(string accountId) => true;
            public bool ActionWithRequestParam(RequestWithAccountId request) => true;

            [SkipMarginTradingEnabledCheck]
            public bool ActionWithSkipAttribute(string accountId) => true;
        }

        public class RequestWithAccountId
        {
            public string AccountId { get; set; }
        }
    }
}