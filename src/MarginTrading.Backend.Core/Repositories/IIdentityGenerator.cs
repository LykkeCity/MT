// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IIdentityGenerator
    {
        Task<long> GenerateIdAsync(string entityType);

        string GenerateGuid();
    }
}