using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class ReportService : IReportService
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IOpenPositionsRepository _openPositionsRepository;
        private readonly IAccountStatRepository _accountStatRepository;

        public ReportService(
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IOpenPositionsRepository openPositionsRepository,
            IAccountStatRepository accountStatRepository)
        {
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _openPositionsRepository = openPositionsRepository;
            _accountStatRepository = accountStatRepository;
        }
        
        public async Task DumpReportData()
        {
            var positions = _ordersCache.GetPositions();
            var accountStat = _accountsCacheService.GetAll();
            
            await Task.WhenAll(
                _openPositionsRepository.Dump(positions), 
                _accountStatRepository.Dump(accountStat));
        }
    }
}