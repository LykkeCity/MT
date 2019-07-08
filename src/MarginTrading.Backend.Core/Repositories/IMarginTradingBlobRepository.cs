// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingBlobRepository
    {
        [CanBeNull]
        T Read<T>(string blobContainer, string key);
        [ItemCanBeNull]
        Task<T> ReadAsync<T>(string blobContainer, string key);
        
        (T, DateTime) ReadWithTimestamp<T>(string blobContainer, string key);
        Task<(T, DateTime)> ReadWithTimestampAsync<T>(string blobContainer, string key);
        
        Task Write<T>(string blobContainer, string key, T obj);
    }
}
