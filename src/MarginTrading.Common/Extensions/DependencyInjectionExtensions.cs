// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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
        /// <exception cref="ArgumentNullException">When context is null</exception>
        /// <returns></returns>
        public static bool TryResolveWithoutException<T>(this IComponentContext context, [NotNullWhen(returnValue: true)] out T? instance) where T: class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            instance = default(T);
            
            try
            {
                if (context.TryResolve(typeof(T), out object? component))
                {
                    instance = (T)component;
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                if (e is InvalidCastException || e is DependencyResolutionException || e is ObjectDisposedException)
                {
                    return false;
                }

                throw;
            }
        }
    }
}