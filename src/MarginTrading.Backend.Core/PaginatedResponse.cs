// Copyright (c) 2019 Lykke Corp.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public class PaginatedResponse<T>
    {
        [NotNull]
        public IReadOnlyList<T> Contents { get; }
        
        public int Start { get; }
        
        public int Size { get; }
        
        public int TotalSize { get; }

        public PaginatedResponse([NotNull] IReadOnlyList<T> contents, int start, int size, int totalSize)
        {
            Contents = contents ?? throw new ArgumentNullException(nameof(contents));
            Start = start;
            Size = size;
            TotalSize = totalSize;
        }
    }
}