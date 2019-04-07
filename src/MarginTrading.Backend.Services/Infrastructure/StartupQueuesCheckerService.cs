using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class StartupQueuesCheckerService
    {
        private readonly MarginTradingSettings _marginTradingSettings;
        
        public StartupQueuesCheckerService(MarginTradingSettings marginTradingSettings)
        {
            _marginTradingSettings = marginTradingSettings;
        }
        
        /// <summary>
        /// Check that RabbitMQ queues of other services which data is required for initialization are empty.
        /// </summary>
        public void Check()
        {
            foreach (var queueName in new[]
            {
                _marginTradingSettings.StartupQueuesChecker.OrderHistoryQueueName,
                _marginTradingSettings.StartupQueuesChecker.PositionHistoryQueueName
            })
            {
                var messageCount = RabbitMqService.GetMessageCount(
                    _marginTradingSettings.StartupQueuesChecker.ConnectionString,
                    queueName);
                if (messageCount == 0)
                {
                    continue;
                }

                throw new Exception(
                    $"All Order/Position broker queues from StartupQueuesChecker setting must be empty. Currently [{queueName}] contains [{messageCount}] messages.");
            }
        }
    }
}