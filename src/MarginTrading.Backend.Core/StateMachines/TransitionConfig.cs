using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;
using MoreLinq;

namespace MarginTrading.Backend.Core.StateMachines
{
    internal static class TransitionConfig
    {
        private static Dictionary<Type, Dictionary<(Enum, Enum), Enum>> Config { get; }

        static TransitionConfig()
        {
            Config = new Dictionary<Type, Dictionary<(Enum, Enum), Enum>>
            {
                {
                    typeof(Order), new Dictionary<(Enum, Enum), Enum>
                    {
                        {(OrderStatus.Placed, OrderCommand.MakeInactive), OrderStatus.Inactive},
                        {(OrderStatus.Placed, OrderCommand.Activate), OrderStatus.Active},
                        {(OrderStatus.Inactive, OrderCommand.Activate), OrderStatus.Active},
                        {(OrderStatus.Placed, OrderCommand.StartExecution), OrderStatus.ExecutionStarted},
                        {(OrderStatus.Active, OrderCommand.StartExecution), OrderStatus.ExecutionStarted},
                        {(OrderStatus.ExecutionStarted, OrderCommand.CancelExecution), OrderStatus.Active},
                        {(OrderStatus.ExecutionStarted, OrderCommand.FinishExecution), OrderStatus.Executed},
                        {(OrderStatus.ExecutionStarted, OrderCommand.Reject), OrderStatus.Rejected},
                        {(OrderStatus.Inactive, OrderCommand.Cancel), OrderStatus.Canceled},
                        {(OrderStatus.Active, OrderCommand.Cancel), OrderStatus.Canceled},
                        {(OrderStatus.Active, OrderCommand.Expire), OrderStatus.Expired},
                    }
                },
                {
                    typeof(Position), new Dictionary<(Enum, Enum), Enum>
                    {
                        {(PositionStatus.Active, PositionCommand.StartClosing), PositionStatus.Closing},
                        {(PositionStatus.Closing, PositionCommand.CancelClosing), PositionStatus.Active},
                        {(PositionStatus.Closing, PositionCommand.Close), PositionStatus.Closed},
                    }
                },
            };
        }

        public static Dictionary<StateTransition<TState, TCommand>, TState> GetConfig<TState, TCommand>(Type objectType)
            where TState : struct, IConvertible
            where TCommand : struct, IConvertible
        {
            return Config[objectType].ToDictionary(
                x => new StateTransition<TState, TCommand>(x.Key.Item1.ToType<TState>(), x.Key.Item2.ToType<TCommand>()), 
                x => x.Value.ToType<TState>());
        }
    }
}