// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lykke.Cqrs;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Base interface for all saga event handlers
    /// </summary>
    public interface ISagaEventHandler<in TEvent>
    {
        Task Handle(TEvent @event, ICommandSender sender);
        
        Task<bool> CanHandle(TEvent @event) => Task.FromResult(true);
    }
}