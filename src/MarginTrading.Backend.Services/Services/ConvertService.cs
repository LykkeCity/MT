// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Percents;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AssetService.Contracts.AssetPair;
using MarginTrading.AssetService.Contracts.TradingConditions;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;
using Microsoft.AspNetCore.Routing;
using VolumePrice = MarginTrading.Backend.Core.Orderbooks.VolumePrice;

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

                cfg.CreateMap<AccountContract, MarginTradingAccount>(MemberList.Source)
                    .ForSourceMember(x => x.ModificationTimestamp, opt => opt.DoNotValidate())
                    .ForMember(d => d.LastUpdateTime, opt => opt.MapFrom(x => x.ModificationTimestamp));

                cfg.CreateMap<MarginTradingAccount, AccountContract>(MemberList.Destination)
                    .ForMember(p => p.ModificationTimestamp,
                        opt => opt.MapFrom(tradingAccount => DateTime.UtcNow));

                cfg.CreateMap<RelatedOrderInfo, RelatedOrderInfoContract>(MemberList.Source);

                cfg.CreateMap<Position, PositionContract>(MemberList.Destination)
                    .ForMember(x => x.TotalPnL, opt => opt.MapFrom(p => p.GetFpl()));

                cfg.CreateMap<AssetPairContract, AssetPair>(MemberList.None);

                cfg.CreateMap<VolumePriceContract, VolumePrice>();
                cfg.CreateMap<ExternalOrderBookContract, ExternalOrderBook>();
                cfg.CreateMap<MarginTrading.OrderbookAggregator.Contracts.Messages.VolumePrice, VolumePrice>();
                cfg.CreateMap<ExternalExchangeOrderbookMessage, ExternalOrderBook>();
            }).CreateMapper();
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