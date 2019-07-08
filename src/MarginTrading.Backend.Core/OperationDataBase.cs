// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core
{
    public class OperationDataBase<TState>
        where TState : struct, IConvertible
    {
        public TState State { get; set; }
    }
}