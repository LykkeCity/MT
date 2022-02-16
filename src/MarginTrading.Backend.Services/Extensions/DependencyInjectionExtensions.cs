// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.Extensions
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Resolves <see cref="IDraftSnapshotKeeper"/> from container and
        /// checks if it was properly initialized, e.g. returns consistent instance
        /// if any. In normal circumstances the <see cref="IDraftSnapshotKeeper"/>
        /// will not be resolved if it was not registered in a container but when
        /// unit testing it is possible to get un-consistent instance created
        /// via reflection, therefore validation is required
        /// </summary>
        /// <param name="context"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool TryResolveSnapshotKeeper(this IComponentContext context,
            [NotNullWhen(returnValue: true)] out IDraftSnapshotKeeper? instance)
        {
            if (context.TryResolveWithoutException(out instance))
            {
                return instance.TradingDay != default(DateTime);
            }

            return false;
        }
    }
}