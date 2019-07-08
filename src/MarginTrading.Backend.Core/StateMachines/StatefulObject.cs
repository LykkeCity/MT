// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Exceptions;

namespace MarginTrading.Backend.Core.StateMachines
{
    /// <summary>
    /// Stateful object abstraction. Represents a simple state machine.
    /// Status change and handler are executed under an object-level lock.
    /// Status must set to initial state in derived type ctor.
    /// Transitions config must be initialized in derived type's static constructor from TransitionConfig.GetConfig.
    /// GetTransitionConfig method must return it's value.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    public abstract class StatefulObject<TState, TCommand>
        where TState : struct, IConvertible
        where TCommand : struct, IConvertible
    {
        public abstract TState Status { get; protected set; }

        private object LockObj { get; } = new object();

        private static Dictionary<StateTransition<TState, TCommand>, TState> TransitionConfig { get; }

        static StatefulObject()
        {
            TransitionConfig = TransitionConfigs.GetConfig<TState, TCommand>(typeof(TState));
        }
        
        private TState GetTransition(TCommand command)
        {
            var transition = new StateTransition<TState, TCommand>(Status, command);

            if (!TransitionConfig.TryGetValue(transition, out var transitionConfig))
            {
                throw new StateTransitionNotFoundException($"Invalid {GetType().Name} transition: {Status} -> {command}");
            }
            
            return transitionConfig;
        }

        protected void ChangeState(TCommand command, Action handler)
        {
            lock (LockObj)
            {
                Status = GetTransition(command);

                handler();
            }
        }
    }
}