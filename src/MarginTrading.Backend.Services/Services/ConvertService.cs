// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Percents;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AssetService.Contracts.AssetPair;
using MarginTrading.AssetService.Contracts.Routes;
using MarginTrading.AssetService.Contracts.TradingConditions;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Common.Services;
using MarginTrading.OrderbookAggregator.Contracts.Messages;
using MarginTrading.SqlRepositories.Entities;
using VolumePrice = MarginTrading.Backend.Core.Orderbooks.VolumePrice;

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class ConvertService : IConvertService
    {
        private readonly IMapper _mapper = CreateMapper();

        private static IMapper CreateMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TradingInstrumentContract, TradingInstrument>()
                    .ForMember(dest => dest.InitLeverage, opt => opt.MapFrom(x => new Leverage(x.InitLeverage)))
                    .ForMember(dest => dest.MaintenanceLeverage,
                        opt => opt.MapFrom(x => new Leverage(x.MaintenanceLeverage)))
                    .ForMember(dest => dest.MarginRate, opt => opt.MapFrom(x => new MarginRate(x.MarginRatePercent)));
            
                cfg.CreateMap<RelatedOrderInfo, RelatedOrderInfoContract>(MemberList.Source);
                cfg.CreateMap<Position, PositionContract>(MemberList.Destination)
                    .ForMember(x => x.TotalPnL, opt => opt.MapFrom(p => p.GetFpl()));

                cfg.CreateMap<AccountContract, MarginTradingAccount>(MemberList.Source)
                        .ForSourceMember(x => x.ModificationTimestamp, opt => opt.DoNotValidate())
                        .ForSourceMember(x => x.ClientModificationTimestamp, opt => opt.DoNotValidate())
                        .ForSourceMember(x => x.ReferenceAccount, opt => opt.DoNotValidate())
                        .ForMember(d => d.LastUpdateTime, opt => opt.MapFrom(x => x.ModificationTimestamp));

                cfg.CreateMap<AssetPairContract, AssetPair>(MemberList.None);

                cfg.CreateMap<VolumePriceContract, VolumePrice>();
                cfg.CreateMap<ExternalOrderBookContract, ExternalOrderBook>();
                cfg.CreateMap<MarginTrading.OrderbookAggregator.Contracts.Messages.VolumePrice, VolumePrice>();
                cfg.CreateMap<ExternalExchangeOrderbookMessage, ExternalOrderBook>();

                cfg.CreateMap<IAccountMarginFreezing, AccountMarginFreezingEntity>();
                cfg.CreateMap<MatchingEngineRouteContract, MatchingEngineRoute>();

                // add all profiles from MT assemblies
                // the idea is to be able to add custom mappings for testing purposes only
                var mtAssemblies = AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Where(a => a.GetName().Name?.StartsWith("MarginTrading") ?? false);
                cfg.AddMaps(mtAssemblies);
                
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
        
        public void AssertConfigurationIsValid()
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }
    }
}