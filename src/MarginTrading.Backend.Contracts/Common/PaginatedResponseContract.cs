// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Common
{
    /// <summary>
    /// Paginated response wrapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginatedResponseContract<T>
    {
        /// <summary>
        /// Paginated sorted contents
        /// </summary>
        [NotNull]
        public IReadOnlyList<T> Contents { get; }
        
        /// <summary>
        /// Start position in total contents
        /// </summary>
        public int Start { get; }
        
        /// <summary>
        /// Size of returned contents
        /// </summary>
        public int Size { get; }
        
        /// <summary>
        /// Total size of all the contents
        /// </summary>
        public long TotalSize { get; }

        public PaginatedResponseContract([NotNull] IReadOnlyList<T> contents, int start, int size, long totalSize)
        {
            Contents = contents ?? throw new ArgumentNullException(nameof(contents));
            Start = start;
            Size = size;
            TotalSize = totalSize;
        }
    }
}