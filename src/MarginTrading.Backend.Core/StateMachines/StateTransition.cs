using System;

namespace MarginTrading.Backend.Core.StateMachines
{
    public class StateTransition<TState, TCommand>
        where TState : struct, IConvertible
        where TCommand : struct, IConvertible
    {
        private readonly TState _currentState;
        private readonly TCommand _command;

        public StateTransition(TState currentState, TCommand command)
        {
            _currentState = currentState;
            _command = command;
        }

        public override int GetHashCode()
        {
            return 17 + 31 * _currentState.GetHashCode() + 31 * _command.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is StateTransition<TState, TCommand> other 
                   && Convert.ToInt32(_currentState) == Convert.ToInt32(other._currentState) 
                   && Convert.ToInt32(_command) == Convert.ToInt32(other._command);
        }
    }
}