// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Rocks.Caching;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Contracts.ErrorCodes;

namespace MarginTrading.Backend.Filters
{
    /// <summary>
    /// Restricts access to actions for clients which are not allowed to use current type of margin trading (live/demo).
    /// If AccountId is not found in the action parameters - does nothing.
    /// </summary>
    public class MarginTradingEnabledFilter : ActionFilterAttribute
    {
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly ICacheProvider _cacheProvider;

        public MarginTradingEnabledFilter(
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService,
            ICacheProvider cacheProvider)
        {
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
            _cacheProvider = cacheProvider;
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ValidateMarginTradingEnabled(context);
            await base.OnActionExecutionAsync(context, next);
        }

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateMarginTradingEnabled(context);
        }

        /// <summary>
        /// Performs a validation if current type of margin trading is enabled globally and for the particular client
        /// (which is extracted from the action parameters).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Using this type of margin trading is restricted for client or
        /// a controller action has more then one AccountId in its parameters.
        /// </exception>
        private void ValidateMarginTradingEnabled(ActionExecutingContext context)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
                return;

            var cacheKey = CacheKeyBuilder.Create(nameof(MarginTradingEnabledFilter), nameof(GetSingleAccountIdGetter),
                controllerActionDescriptor.DisplayName);
            var accountIdGetter = _cacheProvider.Get(cacheKey,
                () => new CachableResult<AccountIdGetter>(GetSingleAccountIdGetter(controllerActionDescriptor),
                    CachingParameters.InfiniteCache));
            if (accountIdGetter != null)
            {
                var accountId = accountIdGetter(context.ActionArguments);
                if (!string.IsNullOrWhiteSpace(accountId))
                {
                    var isAccEnabled = _marginTradingSettingsCacheService.IsMarginTradingEnabledByAccountId(accountId);
                    if (isAccEnabled == null)
                    {
                        throw new InvalidOperationException($"Account {accountId} does not exist");
                    }

                    if (!(bool) isAccEnabled)
                    {
                        throw new InvalidOperationException(
                            $"Using this type of margin trading is restricted for account {accountId}. Error Code: {CommonErrorCodes.AccountDisabled}");
                    }
                }
            }
        }

        /// <summary>
        /// Finds single accountId getter func for current action.
        /// If none found returns null.
        /// If the action is marked to skip the MarginTradingEnabled check also returns null.
        /// </summary>
        [CanBeNull]
        private static AccountIdGetter GetSingleAccountIdGetter(ControllerActionDescriptor controllerActionDescriptor)
        {
            var accountIdGetters = GetAccountIdGetters(controllerActionDescriptor.Parameters).ToList();
            switch (accountIdGetters.Count)
            {
                case 0:
                    return null;
                case 1:
                    return accountIdGetters[0];
                default:
                    throw new InvalidOperationException(
                        "A controller action cannot have more then one AccountId in its parameters");
            }
        }

        /// <summary>
        /// Searches the controller's actions parameters for the presence of AccountId
        /// and returns a func to get the AccountId value from ActionArguments for each of found AccountIds parameters
        /// </summary>
        private static IEnumerable<AccountIdGetter> GetAccountIdGetters(
            IEnumerable<ParameterDescriptor> parameterDescriptors)
        {
            foreach (var parameterDescriptor in parameterDescriptors)
            {
                if (string.Compare(parameterDescriptor.Name, "AccountId", StringComparison.OrdinalIgnoreCase) == 0 &&
                    parameterDescriptor.ParameterType == typeof(string))
                {
                    yield return d => d.TryGetValue(parameterDescriptor.Name, out var arg) ? (string) arg : null;
                }
                else
                {
                    var accountIdPropertyInfo =
                        parameterDescriptor.ParameterType.GetProperty("AccountId", typeof(string));
                    if (accountIdPropertyInfo != null)
                    {
                        yield return d =>
                            d.TryGetValue(parameterDescriptor.Name, out var arg)
                                ? (string) ((dynamic) arg)?.AccountId
                                : null;
                    }
                }
            }
        }

        private delegate string AccountIdGetter(IDictionary<string, object> actionArguments);
    }
}