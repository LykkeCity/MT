﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Filters;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
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
        public void ActionWithoutClientId_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService = Mock.Of<IMarginTradingSettingsService>(s => s.IsMarginTradingEnabled("id of client", It.IsAny<bool>()) == Task.FromResult(false));
            var sut = new MarginTradingEnabledFilter(new MarginSettings(), marginTradingSettingsService, new DummyCacheProvider());

            //act
            var context = new ActionExecutingContext(new ControllerContext
                                                     {
                                                         ActionDescriptor = new ControllerActionDescriptor
                                                         {
                                                             DisplayName = "action display name",
                                                             Parameters = new List<ParameterDescriptor> { new ControllerParameterDescriptor { Name = "i", ParameterType = typeof(int), } },
                                                             MethodInfo = typeof(TestController).GetMethod("ActionWithoutClientId"),
                                                         },
                                                         HttpContext = new DefaultHttpContext(),
                                                         RouteData = new RouteData(),
                                                     },
                                                     new List<IFilterMetadata>(),
                                                     new Dictionary<string, object>(),
                                                     new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.ShouldNotThrow();
        }


        [Test]
        public void ActionWithClientIdParam_IfTradingEnabled_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService = Mock.Of<IMarginTradingSettingsService>(s => s.IsMarginTradingEnabled("id of client", It.IsAny<bool>()) == Task.FromResult(true));
            var sut = new MarginTradingEnabledFilter(new MarginSettings(), marginTradingSettingsService, new DummyCacheProvider());

            //act
            var context = new ActionExecutingContext(new ControllerContext
                                                     {
                                                         ActionDescriptor = new ControllerActionDescriptor
                                                         {
                                                             DisplayName = "action display name",
                                                             Parameters = new List<ParameterDescriptor> { new ControllerParameterDescriptor { Name = "clientId", ParameterType = typeof(string), } },
                                                             MethodInfo = typeof(TestController).GetMethod("ActionWithClientIdParam"),
                                                         },
                                                         HttpContext = new DefaultHttpContext(),
                                                         RouteData = new RouteData(),
                                                     },
                                                     new List<IFilterMetadata>(),
                                                     new Dictionary<string, object> { { "clientId", "id of client" } },
                                                     new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.ShouldNotThrow();
        }


        [Test]
        public void ActionWithClientIdParam_IfTradingDisabled_ShouldThrow()
        {
            //arrange
            var marginTradingSettingsService = Mock.Of<IMarginTradingSettingsService>(s => s.IsMarginTradingEnabled("id of client", It.IsAny<bool>()) == Task.FromResult(false));
            var sut = new MarginTradingEnabledFilter(new MarginSettings(), marginTradingSettingsService, new DummyCacheProvider());

            //act
            var context = new ActionExecutingContext(new ControllerContext
                                                     {
                                                         ActionDescriptor = new ControllerActionDescriptor
                                                         {
                                                             DisplayName = "action display name",
                                                             Parameters = new List<ParameterDescriptor> { new ControllerParameterDescriptor { Name = "clientId", ParameterType = typeof(string), } },
                                                             MethodInfo = typeof(TestController).GetMethod("ActionWithClientIdParam"),
                                                         },
                                                         HttpContext = new DefaultHttpContext(),
                                                         RouteData = new RouteData(),
                                                     },
                                                     new List<IFilterMetadata>(),
                                                     new Dictionary<string, object> { { "clientId", "id of client" } },
                                                     new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.ShouldThrow<InvalidOperationException>().WithMessage("Using this type of margin trading is restricted for client");
        }


        [Test]
        public void ActionWithRequestParam_IfTradingEnabled_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService = Mock.Of<IMarginTradingSettingsService>(s => s.IsMarginTradingEnabled("id of client", It.IsAny<bool>()) == Task.FromResult(true));
            var sut = new MarginTradingEnabledFilter(new MarginSettings(), marginTradingSettingsService, new DummyCacheProvider());

            //act
            var context = new ActionExecutingContext(new ControllerContext
                                                     {
                                                         ActionDescriptor = new ControllerActionDescriptor
                                                         {
                                                             DisplayName = "action display name",
                                                             Parameters = new List<ParameterDescriptor> { new ControllerParameterDescriptor { Name = "request", ParameterType = typeof(RequestWithClientId), } },
                                                             MethodInfo = typeof(TestController).GetMethod("ActionWithRequestParam"),
                                                         },
                                                         HttpContext = new DefaultHttpContext(),
                                                         RouteData = new RouteData(),
                                                     },
                                                     new List<IFilterMetadata>(),
                                                     new Dictionary<string, object> { { "request", new RequestWithClientId { ClientId = "id of client" } } },
                                                     new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.ShouldNotThrow();
        }


        [Test]
        public void ActionWithRequestParam_IfTradingDisabled_ShouldThrow()
        {
            //arrange
            var marginTradingSettingsService = Mock.Of<IMarginTradingSettingsService>(s => s.IsMarginTradingEnabled("id of client", It.IsAny<bool>()) == Task.FromResult(false));
            var sut = new MarginTradingEnabledFilter(new MarginSettings(), marginTradingSettingsService, new DummyCacheProvider());

            //act
            var context = new ActionExecutingContext(new ControllerContext
                                                     {
                                                         ActionDescriptor = new ControllerActionDescriptor
                                                         {
                                                             DisplayName = "action display name",
                                                             Parameters = new List<ParameterDescriptor> { new ControllerParameterDescriptor { Name = "request", ParameterType = typeof(RequestWithClientId), } },
                                                             MethodInfo = typeof(TestController).GetMethod("ActionWithRequestParam"),
                                                         },
                                                         HttpContext = new DefaultHttpContext(),
                                                         RouteData = new RouteData(),
                                                     },
                                                     new List<IFilterMetadata>(),
                                                     new Dictionary<string, object> { { "request", new RequestWithClientId { ClientId = "id of client" } } },
                                                     new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.ShouldThrow<InvalidOperationException>().WithMessage("Using this type of margin trading is restricted for client");
        }


        [Test]
        public void ActionWithSkipAttribute_IfTradingDisabled_ShouldNotThrow()
        {
            //arrange
            var marginTradingSettingsService = Mock.Of<IMarginTradingSettingsService>(s => s.IsMarginTradingEnabled("id of client", It.IsAny<bool>()) == Task.FromResult(false));
            var sut = new MarginTradingEnabledFilter(new MarginSettings(), marginTradingSettingsService, new DummyCacheProvider());

            //act
            var context = new ActionExecutingContext(new ControllerContext
                                                     {
                                                         ActionDescriptor = new ControllerActionDescriptor
                                                         {
                                                             DisplayName = "action display name",
                                                             Parameters = new List<ParameterDescriptor> { new ControllerParameterDescriptor { Name = "clientId", ParameterType = typeof(string), } },
                                                             MethodInfo = typeof(TestController).GetMethod("ActionWithSkipAttribute"),
                                                         },
                                                         HttpContext = new DefaultHttpContext(),
                                                         RouteData = new RouteData(),
                                                     },
                                                     new List<IFilterMetadata>(),
                                                     new Dictionary<string, object> { { "clientId", "id of client" } },
                                                     new TestController());

            Func<Task> invocation = () => sut.OnActionExecutionAsync(context, NextFunc);

            //assert
            invocation.ShouldNotThrow();
        }


        private static Task<ActionExecutedContext> NextFunc()
        {
            return Task.FromResult(new ActionExecutedContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()), new List<IFilterMetadata>(), new TestController()));
        }

        private class TestController
        {
            public bool ActionWithoutClientId(int i) => true;
            public bool ActionWithClientIdParam(string clientId) => true;
            public bool ActionWithRequestParam(RequestWithClientId request) => true;

            [SkipMarginTradingEnabledCheck]
            public bool ActionWithSkipAttribute(string clientId) => true;
        }

        public class RequestWithClientId
        {
            public string ClientId { get; set; }
        }
    }
}
