// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class CloseAccountPositionsResponse
    {
        public List<CloseAccountPositionsResult> Results { get; set; }
    }
}