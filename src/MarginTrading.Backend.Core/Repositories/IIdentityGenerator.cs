// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IIdentityGenerator
    {
        Task<long> GenerateIdAsync(string entityType);

        string GenerateAlphanumericId();
        
        string GenerateGuid();
    }
}