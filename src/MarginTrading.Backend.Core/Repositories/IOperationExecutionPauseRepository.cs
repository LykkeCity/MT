// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IOperationExecutionPauseRepository
    {
        Task AddAsync(Pause pause);
        
        Task<IEnumerable<Pause>> FindAsync(string operationId, string operationName, Func<Pause, bool> filter = null);

        Task<bool> UpdateAsync(long oid,
            DateTime effectiveSince,
            PauseState state,
            DateTime? cancelledAt,
            DateTime? cancellationEffectiveSince,
            Initiator? cancellationInitiator,
            PauseCancellationSource? cancellationSource);
    }
}