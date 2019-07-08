// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IAccountStatRepository
    {
        Task Dump(IEnumerable<MarginTradingAccount> accounts);
    }
}