// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    /// <summary>
    /// The service gather all descendants of AbstractMigration class and invoke them in StartApplicationAsync.
    /// </summary>
    public interface IMigrationService
    {
        Task InvokeAll();
    }
}