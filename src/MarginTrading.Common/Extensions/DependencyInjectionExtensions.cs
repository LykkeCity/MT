// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Core;

namespace MarginTrading.Common.Extensions
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Original methods TryResolve and ResolveOptional raise <see cref="DependencyResolutionException"/> if
        /// component can't be resolved. "Try" part is around testing whether component is registered in DI container
        /// or not but if it can't be resolved - there will be an exception. TryResolveWithoutException method hides the
        /// exception and just returns false in this case.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryResolveWithoutException<T>(this IComponentContext context, out T instance)
        {
            try
            {
                return context.TryResolve<T>(out instance);
            }
            catch (DependencyResolutionException e)
            {
                instance = default(T);
                return false;
            }
        }
    }
}