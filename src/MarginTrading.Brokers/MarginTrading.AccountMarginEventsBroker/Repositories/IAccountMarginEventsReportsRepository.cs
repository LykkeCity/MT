﻿using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using System.Threading.Tasks;

namespace MarginTrading.AccountMarginEventsBroker.Repositories
{
    internal interface IAccountMarginEventsReportsRepository
    {
        Task InsertOrReplaceAsync(IAccountMarginEventReport report);
    }
}