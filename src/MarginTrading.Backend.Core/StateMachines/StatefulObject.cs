using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Exceptions;

namespace MarginTrading.Backend.Core.StateMachines
{
    /// <summary>
    /// Stateful object abstraction. Represents a simple state machine.
    /// In derived type ctor initialization must be performed: Status set to initial state and Transitions configured.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    public abstract class StatefulObject<TState, TCommand>
        where TState : struct, IConvertible
        where TCommand : struct, IConvertible
    {
        public abstract TState Status { get; protected set; }
        
        protected abstract Dictionary<StateTransition<TState, TCommand>, TState> Transitions { get; }

        private object LockObj { get; } = new object();

        private TState GetTransition(TCommand command)
        {
            var transition = new StateTransition<TState, TCommand>(Status, command);
            
            if (!Transitions.TryGetValue(transition, out var transitionConfig))
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