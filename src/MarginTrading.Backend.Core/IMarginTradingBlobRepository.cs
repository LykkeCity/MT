﻿using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingBlobRepository
    {
        [CanBeNull]
        T Read<T>(string blobContainer, string key);
        Task Write<T>(string blobContainer, string key, T obj);
        [ItemCanBeNull]
        Task<T> ReadAsync<T>(string blobContainer, string key);
    }
}
