// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Base interface for all saga event handlers
    /// </summary>
    public interface ISagaEventHandler
    {
        Task Handle(object @event, ICommandSender sender);
    }
    
    /// <summary>
    /// Marker interface for special liquidation saga only event handlers
    /// </summary>
    public interface ISpecialLiquidationSagaEventHandler : ISagaEventHandler
    {
    }

    /// <summary>
    /// Base class for special liquidation saga event handler implementations
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class SpecialLiquidationSagaEventHandler<TEvent> : ISpecialLiquidationSagaEventHandler
    {
        public Task Handle(object @event, ICommandSender sender)
        {
            if (@event is TEvent typedEvent)
            {
                return HandleEvent(typedEvent, sender);
            }
            
            return Task.CompletedTask;
        }
        
        protected abstract Task HandleEvent(TEvent @event, ICommandSender sender);
    }

    /// <summary>
    /// Event handler implementation for <see cref="SpecialLiquidationFailedEvent"/>
    /// </summary>
    public class SpecialLiquidationFailedEventHandler : SpecialLiquidationSagaEventHandler<SpecialLiquidationFailedEvent>
    {
        private readonly ILogger<SpecialLiquidationFailedEventHandler> _logger;

        public SpecialLiquidationFailedEventHandler(ILogger<SpecialLiquidationFailedEventHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleEvent(SpecialLiquidationFailedEvent @event, ICommandSender sender)
        {
            _logger.LogInformation("Special liquidation failed {@event}", @event);
            return Task.CompletedTask;
        }
    }

    public static class EventHandlerExtensions
    {
        public static async Task HandleEvent(this IEnumerable<ISpecialLiquidationSagaEventHandler> handlers, 
            object @event, 
            ICommandSender sender)
        {
            foreach (var handler in handlers)
            {
                await handler.Handle(@event, sender);
            }
        }
    }
}