// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class CloseAccountPositionsResponse
    {
        public List<CloseAccountPositionsResult> Results { get; set; }
    }
}