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
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services.Settings;

namespace MarginTrading.Backend.Filters
{
    /// <summary>
    /// Restricts access to actions for clients which are not allowed to use current type of margin trading (live/demo).
    /// Skips validation if current action method is marked with <see cref="SkipMarginTradingEnabledCheckAttribute"/>.
    /// If AccountId is not found in the action parameters - does nothing.
    /// </summary>
    public class MarginTradingEnabledFilter : ActionFilterAttribute
    {
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly ICacheProvider _cacheProvider;
        private readonly ILog _log;

        public MarginTradingEnabledFilter(IMarginTradingSettingsCacheService marginTradingSettingsCacheService,
            ICacheProvider cacheProvider, ILog log)
        {
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
            _cacheProvider = cacheProvider;
            _log = log;
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ValidateMarginTradingEnabledAsync(context);
            await base.OnActionExecutionAsync(context, next);
        }

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateMarginTradingEnabledAsync(context);
        }

        /// <summary>
        /// Performs a validation if current type of margin trading is enabled globally and for the particular client
        /// (which is extracted from the action parameters).
        /// Skips validation if current action method is marked with <see cref="SkipMarginTradingEnabledCheckAttribute"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Using this type of margin trading is restricted for client or
        /// a controller action has more then one AccountId in its parameters.
        /// </exception>
        private void ValidateMarginTradingEnabledAsync(ActionExecutingContext context)
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
                            "Using this type of margin trading is restricted for account id " + accountId);
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
            if (ActionHasSkipCkeckAttribute(controllerActionDescriptor))
                return null;

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

        private static bool ActionHasSkipCkeckAttribute(ControllerActionDescriptor controllerActionDescriptor)
        {
            return controllerActionDescriptor.MethodInfo.GetCustomAttribute<SkipMarginTradingEnabledCheckAttribute>() !=
                null;
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