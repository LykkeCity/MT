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
                        {(OrderStatus.Placed, OrderStatus.Inactive), OrderCommand.MakeInactive},
                        {(OrderStatus.Placed, OrderStatus.Active), OrderCommand.Activate},
                        {(OrderStatus.Inactive, OrderStatus.Active), OrderCommand.Activate},
                        {(OrderStatus.Placed, OrderStatus.ExecutionStarted), OrderCommand.StartExecution},
                        {(OrderStatus.Active, OrderStatus.ExecutionStarted), OrderCommand.StartExecution},
                        {(OrderStatus.ExecutionStarted, OrderStatus.Active), OrderCommand.CancelExecution},
                        {(OrderStatus.ExecutionStarted, OrderStatus.Executed), OrderCommand.FinishExecution},
                        {(OrderStatus.ExecutionStarted, OrderStatus.Rejected), OrderCommand.Reject},
                        {(OrderStatus.Inactive, OrderStatus.Canceled), OrderCommand.Cancel},
                        {(OrderStatus.Active, OrderStatus.Canceled), OrderCommand.Cancel},
                        {(OrderStatus.Active, OrderStatus.Expired), OrderCommand.Expire},
                    }
                },
                {
                    typeof(Position), new Dictionary<(Enum, Enum), Enum>
                    {
                        {(PositionStatus.Active, PositionStatus.Closing), PositionCommand.StartClosing},
                        {(PositionStatus.Closing, PositionStatus.Active), PositionCommand.CancelClosing},
                        {(PositionStatus.Closing, PositionStatus.Closed), PositionCommand.Close},
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