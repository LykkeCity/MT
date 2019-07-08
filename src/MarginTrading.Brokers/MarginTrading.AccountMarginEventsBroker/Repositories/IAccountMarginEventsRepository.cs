// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using System.Threading.Tasks;

namespace MarginTrading.AccountMarginEventsBroker.Repositories
{
    internal interface IAccountMarginEventsRepository
    {
        Task InsertOrReplaceAsync(IAccountMarginEvent report);
    }
}