// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core
{
    public interface IOperationExecutionInfo<T> where T: class
    {
        string OperationName { get; }
        string Id { get; }
        DateTime LastModified { get; }

        T Data { get; }
    }
}