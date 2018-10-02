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
        private readonly IConvertService _convertService;
        private readonly IOpenPositionsRepository _openPositionsRepository;

        public ReportService(
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IConvertService convertService,
            IOpenPositionsRepository openPositionsRepository)
        {
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _convertService = convertService;
            _openPositionsRepository = openPositionsRepository;
        }
        
        public async Task DumpReportData()
        {
            var positions = _ordersCache.GetPositions();
            
            await _openPositionsRepository.Dump(positions.Select(position =>
            {
                var positionContract = _convertService.Convert<Position, PositionContract>(position,
                    o => o.ConfigureMap(MemberList.Destination).ForMember(x => x.TotalPnL, c => c.Ignore()));
                positionContract.TotalPnL = position.GetFpl();
                return positionContract;
            }));
            
            var accountStat = _accountsCacheService.GetAll();

        }
    }
}