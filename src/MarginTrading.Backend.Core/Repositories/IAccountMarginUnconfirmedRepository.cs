// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IAccountMarginUnconfirmedRepository
    {
        Task<IReadOnlyList<IAccountMarginFreezing>> GetAllAsync();
        Task<bool> TryInsertAsync(IAccountMarginFreezing item);
        Task DeleteAsync(string operationId);
    }
}