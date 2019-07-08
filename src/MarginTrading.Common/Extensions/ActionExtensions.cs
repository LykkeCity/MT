// Copyright (c) 2019 Lykke Corp.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Common.Extensions
{
    public static class ActionExtensions
    {
        /// <summary>
        ///     <para>
        ///         Retries <paramref name="action" /> maximum <paramref name="maxRetries" /> times
        ///         if catched exceptions are retriable (<paramref name="isRetriableException" /> returns true for them).
        ///     </para>
        ///     <para>
        ///         Optionally logs each applicable exception before retrying the <paramref name="action" />
        ///         to specified <paramref name="logException" /> callback passing the exception and current
        ///         retry attempt count (starting with 1).
        ///     </para>
        ///     <para>
        ///         If <paramref name="backoffTime"/> is specified then the processing pauses for the specified time after
        ///     </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> RetryOnExceptionAsync<T>([NotNull] this Func<Task<T>> action,
                                                                [NotNull] Func<Exception, bool> isRetriableException,
                                                                int maxRetries,
                                                                TimeSpan? backoffTime = null,
                                                                Action<Exception> logException = null)
        {
            action.RequiredNotNull(nameof(action));
            isRetriableException.RequiredNotNull(nameof(isRetriableException));

            var retries = 0;
            while (true)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    retries++;

                    if (retries > maxRetries || !isRetriableException(ex))
                    {
                        throw;
                    }

                    logException?.Invoke(ex);
                }

                if (backoffTime != null)
                {
                    await Task.Delay(backoffTime.Value);
                }
            }
        }
    }
}
