// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class PositionHistoryHandler : IPositionHistoryHandler
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IConvertService _convertService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;

        public PositionHistoryHandler(ICqrsSender cqrsSender,
            IConvertService convertService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IDateService dateService,
            IAccountsCacheService accountsCacheService)
        {
            _cqrsSender = cqrsSender;
            _convertService = convertService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
        }

        public Task HandleOpenPosition(Position position, string additionalInfo, PositionOpenMetadata metadata)
        {
            var historyEvent = CreatePositionHistoryEvent(position, 
                PositionHistoryTypeContract.Open, 
                null,
                additionalInfo, 
                metadata.ToJson());
            return _rabbitMqNotifyService.PositionHistory(historyEvent);
        }

        public Task HandleClosePosition(Position position, DealContract deal, string additionalInfo)
        {
            var positionClosedEvent = CreatePositionClosedEvent(position, deal);
            _cqrsSender.PublishEvent(positionClosedEvent);

            var historyEvent = CreatePositionHistoryEvent(position, 
                PositionHistoryTypeContract.Close, 
                deal,
                additionalInfo);
            return _rabbitMqNotifyService.PositionHistory(historyEvent);
        }
        
        public Task HandlePartialClosePosition(Position position, DealContract deal, string additionalInfo)
        {
            var positionClosedEvent = CreatePositionClosedEvent(position, deal);
            _cqrsSender.PublishEvent(positionClosedEvent);

            var historyEvent = CreatePositionHistoryEvent(position, 
                PositionHistoryTypeContract.PartiallyClose, 
                deal,
                additionalInfo);
            return _rabbitMqNotifyService.PositionHistory(historyEvent);
        }
        
        private PositionHistoryEvent CreatePositionHistoryEvent(Position position,
            PositionHistoryTypeContract type,
            DealContract deal = null,
            string additionalInfo = null,
            string metadata = null)
        {
            var positionSnapshot = _convertService.Convert<Position, PositionContract>(position);

            return new PositionHistoryEvent
            {
                PositionSnapshot = positionSnapshot,
                Deal = deal,
                EventType = type,
                Timestamp = _dateService.Now(),
                ActivitiesMetadata = metadata,
                OrderAdditionalInfo = additionalInfo
            };
        }
        
        private PositionClosedEvent CreatePositionClosedEvent(Position position, DealContract deal)
        {
            var account = _accountsCacheService.Get(position.AccountId);
            
            return new PositionClosedEvent(account.Id, 
                account.ClientId, 
                deal.DealId, 
                position.AssetPairId, 
                deal.PnlOfTheLastDay);
        }
    }
}