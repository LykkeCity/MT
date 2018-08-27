using System;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class SagaExtensions
    {
        public static bool SwitchState(this OperationData data, 
            OperationState expectedState, OperationState nextState)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Operation execution data was not properly initialized.");
            }
            
            if (data.State < expectedState)
            {
                // Throws to retry and wait until the operation will be in the required state
                throw new InvalidOperationException(
                    $"Operation execution state can't be switched: {data.State} -> {nextState}. Waiting for the {expectedState} state.");
            }

            if (data.State > expectedState)
            {
                // Already in the next state, so this event can be just ignored
                return false;
            }

            data.State = nextState;

            return true;
        }
    }
}