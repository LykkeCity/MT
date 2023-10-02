// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using AutoMapper;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;

namespace MarginTradingTests
{
    /// <summary>
    /// Mapping profiles for testing purposes only
    /// </summary>
    [UsedImplicitly]
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MarginTradingAccount, AccountContract>(MemberList.Destination)
                .ForMember(p => p.ReferenceAccount, opt => opt.Ignore())
                .ForMember(p => p.ClientModificationTimestamp, opt => opt.Ignore())
                .ForMember(p => p.ModificationTimestamp,
                    opt => opt.MapFrom(tradingAccount => DateTime.UtcNow));
        }
    }
}