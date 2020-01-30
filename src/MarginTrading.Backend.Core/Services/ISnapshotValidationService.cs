// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Core.Services
{
    /// <summary>
    /// Responsible for validation of current state using the latest snapshot and history.
    /// </summary>
    public interface ISnapshotValidationService
    {
        /// <summary>
        /// Validates current trading state for inconsistent state.
        /// </summary>
        /// <returns>The validation result that contains the details of the mismatch between the current and expected status..</returns>
        Task<SnapshotValidationResult> ValidateCurrentStateAsync();
    }
}