// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common;
using Common.Log;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class SagaExtensions
    {
        public static bool SwitchState<TState>(this OperationDataBase<TState> data, TState expectedState,
            TState nextState)
            where TState : struct, IConvertible
        {
            if (data == null)
            {
                throw new InvalidOperationException("Operation execution data was not properly initialized.");
            }

            if (Convert.ToInt32(data.State) < Convert.ToInt32(expectedState))
            {
                // Throws to retry and wait until the operation will be in the required state
                throw new InvalidOperationException(
                    $"Operation execution state can't be switched: {data.State} -> {nextState}. Waiting for the {expectedState} state.");
            }

            if (Convert.ToInt32(data.State) > Convert.ToInt32(expectedState))
            {
                LogLocator.CommonLog.WriteWarning(nameof(SagaExtensions), nameof(SwitchToState),
                    $"Operation is already in the next state, so this event is ignored, {new {data, expectedState, nextState}.ToJson()}.");
                return false;
            }

            data.State = nextState;

            return true;
        }

        public static bool SwitchToState(this OperationDataBase<SpecialLiquidationOperationState> data,
            SpecialLiquidationOperationState nextState) =>
            data.SwitchState(data.State, nextState);
        
        public static bool SwitchToState(this IOperationExecutionInfo<SpecialLiquidationOperationData> info,
            SpecialLiquidationOperationState nextState) =>
            info.Data.SwitchToState(nextState);

        public static bool SwitchState(this OperationDataBase<SpecialLiquidationOperationState> data,
            SpecialLiquidationOperationState expectedState, SpecialLiquidationOperationState nextState)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Operation execution data was not properly initialized.");
            }

            if (Convert.ToInt32(data.State) < Convert.ToInt32(expectedState))
            {
                // Throws to retry and wait until the operation will be in the required state
                throw new InvalidOperationException(
                    $"Operation execution state can't be switched: {data.State} -> {nextState}. Waiting for the {expectedState} state.");
            }

            if (Convert.ToInt32(data.State) > Convert.ToInt32(expectedState))
            {
                LogLocator.CommonLog.WriteWarning(nameof(SagaExtensions), nameof(SwitchToState),
                    $"Operation is already in the next state, so this event is ignored, {new {data, expectedState, nextState}.ToJson()}.");
                return false;
            }

            if (data.State == SpecialLiquidationOperationState.Failed &&
                nextState == SpecialLiquidationOperationState.Cancelled)
            {
                LogLocator.CommonLog.WriteWarning(nameof(SagaExtensions), nameof(SwitchToState),
                    $"Cannot switch from Failed to Cancelled state (both states are final), so this event is ignored, {new {data, expectedState, nextState}.ToJson()}.");
                return false;
            }

            data.State = nextState;

            return true;
        }
    }
}