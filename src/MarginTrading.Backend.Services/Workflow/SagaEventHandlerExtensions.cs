// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Cqrs;

namespace MarginTrading.Backend.Services.Workflow
{
    public static class SagaEventHandlerExtensions
    {
        /// <summary>
        /// Find handler which implements ISagaEventHandler{TEvent} by convention
        /// </summary>
        /// <param name="handlers"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">When no handler found</exception>
        public static ISagaEventHandler<TEvent> First<TEvent>(
            this IEnumerable<ISpecialLiquidationSagaEventHandler> handlers)
        {
            foreach (var handler in handlers)
            {
                if (handler is ISagaEventHandler<TEvent> typedHandler)
                {
                    return typedHandler;
                }
            }
            
            throw new KeyNotFoundException($"No handler for {typeof(TEvent)} found");
        }

        /// <summary>
        /// Finds handler which implements ISagaEventHandler{TEvent} by convention and calls it
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        /// <remarks>Before calling handler checks if it can handle event</remarks>
        public static async Task Handle<TEvent>(
            this IEnumerable<ISpecialLiquidationSagaEventHandler> handlers,
            TEvent @event,
            ICommandSender sender)
        {
            var firstHandler = handlers.First<TEvent>();

            if (await firstHandler.CanHandle(@event))
                await firstHandler.Handle(@event, sender);
        }
    }
}