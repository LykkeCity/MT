// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Percents;
using MarginTrading.AssetService.Contracts.TradingConditions;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Routing;

namespace MarginTrading.Backend.Services
{
    [UsedImplicitly]
    public class ConvertService : IConvertService
    {
        private readonly IMapper _mapper = CreateMapper();

        private readonly ConcurrentDictionary<(Type Source, Type Result), Type> _constructorArgsTypes =
            new ConcurrentDictionary<(Type Source, Type Destination), Type>();

        private static IMapper CreateMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TradingInstrumentContract, TradingInstrument>()
                    .ForMember(dest => dest.InitLeverage, opt => opt.MapFrom(x => new Leverage(x.InitLeverage)))
                    .ForMember(dest => dest.MaintenanceLeverage, opt => opt.MapFrom(x => new Leverage(x.MaintenanceLeverage)))
                    .ForMember(dest => dest.MarginRate, opt => opt.MapFrom(x => new MarginRate(x.MarginRatePercent)));
                
                cfg.CreateMap<Position, PositionContract>(MemberList.Destination)
                    .ForMember(x => x.TotalPnL, opt => opt.MapFrom(p => p.GetFpl()));
                
            }).CreateMapper();
        }

        public TResult Convert<TSource, TResult>(TSource source,
            Action<IMappingOperationOptions<TSource, TResult>> opts)
        {
            return _mapper.Map(source, opts);
        }

        public TResult ConvertWithConstructorArgs<TSource, TResult>(TSource source, object argumentsObject)
        {
            _constructorArgsTypes.AddOrUpdate((typeof(TSource), typeof(TResult)), k => argumentsObject.GetType(),
                (k, old) => argumentsObject.GetType()
                    .RequiredEqualsTo(old, "argumentsObject should always be of the same type"));
            var arguments = GetProperties(argumentsObject);
            return _mapper.Map<TSource, TResult>(source, o =>
            {
                var conf = o.ConfigureMap();
                foreach (var pair in arguments)
                {
                    conf.ForCtorParam(pair.Key,
                        e => e.ResolveUsing((contract, context) => (string) context.Items[pair.Key]));
                    o.Items[pair.Key] = pair.Value;
                }
            });
        }

        public TResult Convert<TSource, TResult>(TSource source)
        {
            return _mapper.Map<TSource, TResult>(source);
        }

        public TResult Convert<TResult>(object source)
        {
            return _mapper.Map<TResult>(source);
        }

        /// <summary>
        /// Get the properties and values of an object using an existing opimized implementation 
        /// </summary>
        private static IReadOnlyDictionary<string, object> GetProperties(object obj)
        {
            return new RouteValueDictionary(obj);
        }
    }
}