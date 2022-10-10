// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using AutoMapper;

namespace MarginTrading.Common.Services
{
    public interface IConvertService
    {
        TResult Convert<TSource, TResult>(TSource source);
        TResult Convert<TResult>(object source);
    }
}