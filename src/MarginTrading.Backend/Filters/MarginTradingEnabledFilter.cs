﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Rocks.Caching;
using MarginTrading.Backend.Attributes;

namespace MarginTrading.Backend.Filters
{
    /// <summary>
    /// Restricts access to actions for clients which are not allowed to use current type of margin trading (live/demo).
    /// Skips validation if current action method is marked with <see cref="SkipMarginTradingEnabledCheckAttribute"/>.
    /// If ClientId is not found in the action parameters - does nothing.
    /// </summary>
    public class MarginTradingEnabledFilter: ActionFilterAttribute
    {
        private readonly MarginSettings _marginSettings;
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;
        private readonly ICacheProvider _cacheProvider;

        public MarginTradingEnabledFilter(
            MarginSettings marginSettings,
            IMarginTradingSettingsService marginTradingSettingsService,
            ICacheProvider cacheProvider)
        {
            _marginSettings = marginSettings;
            _marginTradingSettingsService = marginTradingSettingsService;
            _cacheProvider = cacheProvider;
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await ValidateMarginTradingEnabledAsync(context);
            await base.OnActionExecutionAsync(context, next);
        }

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // this won't be called frequently as we have most actions async
            ValidateMarginTradingEnabledAsync(context).Wait();
        }

        /// <summary>
        /// Performs a validation if current type of margin trading is enabled globally and for the particular client
        /// (which is extracted from the action parameters).
        /// Skips validation if current action method is marked with <see cref="SkipMarginTradingEnabledCheckAttribute"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Using this type of margin trading is restricted for client or
        /// a controller action has more then one ClientId in its parameters.
        /// </exception>
        private async Task ValidateMarginTradingEnabledAsync(ActionExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null)
            {
                return;
            }

            var cacheKey = CacheKeyBuilder.Create(nameof(MarginTradingEnabledFilter), nameof(GetSingleClientIdGetter), controllerActionDescriptor.DisplayName);
            var clientIdGetter = _cacheProvider.Get(cacheKey, () => new CachableResult<ClientIdGetter>(GetSingleClientIdGetter(controllerActionDescriptor), CachingParameters.InfiniteCache));
            if (clientIdGetter != null)
            {
                var clientId = clientIdGetter(context.ActionArguments);
                if (!await _marginTradingSettingsService.IsMarginTradingEnabled(clientId, _marginSettings.IsLive))
                {
                    throw new InvalidOperationException("Using this type of margin trading is restricted for client");
                }
            }
        }

        /// <summary>
        /// Finds single clientId getter func for current action.
        /// If none found returns null.
        /// If the action is marked to skip the MarginTradingEnabled check also returns null.
        /// </summary>
        [CanBeNull]
        private static ClientIdGetter GetSingleClientIdGetter(ControllerActionDescriptor controllerActionDescriptor)
        {
            if (controllerActionDescriptor.MethodInfo.GetCustomAttribute<SkipMarginTradingEnabledCheckAttribute>() != null)
            {
                return null;
            }

            var clientIdGetters = GetClientIdGetters(controllerActionDescriptor.Parameters).ToList();
            switch (clientIdGetters.Count)
            {
                case 0:
                    return null;
                case 1:
                    return clientIdGetters[0];
                default:
                    throw new InvalidOperationException("A controller action cannot have more then one ClientId in its parameters");
            }
        }

        /// <summary>
        /// Searches the controller's actions parameters for the presence of ClientId
        /// and returns a func to get the ClientId value from ActionArguments for each of found ClientIds parameters
        /// </summary>
        private static IEnumerable<ClientIdGetter> GetClientIdGetters(IList<ParameterDescriptor> parameterDescriptors)
        {
            foreach (var parameterDescriptor in parameterDescriptors)
            {
                if (string.Compare(parameterDescriptor.Name, "ClientId", StringComparison.OrdinalIgnoreCase) == 0
                    && parameterDescriptor.ParameterType == typeof(string))
                {
                    yield return d => (string) d[parameterDescriptor.Name];
                }
                else
                {
                    var clientIdPropertyInfo = parameterDescriptor.ParameterType.GetProperty("ClientId", typeof(string));
                    if (clientIdPropertyInfo != null)
                    {
                        yield return d => (string) ((dynamic) d[parameterDescriptor.Name]).ClientId;
                    }
                }
            }
        }

        private delegate string ClientIdGetter(IDictionary<string, object> actionArguments);
    }
}
