using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Contracts.RabbitMqMessages;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Settings;
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader
{
    public class Application : IStartable
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly DataReaderSettings _dataReaderSettings;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;

        public Application(IRabbitMqService rabbitMqService, DataReaderSettings dataReaderSettings,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService)
        {
            _rabbitMqService = rabbitMqService;
            _dataReaderSettings = dataReaderSettings;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
            ;
        }

        public void Start()
        {
            _rabbitMqService.Subscribe(
                _dataReaderSettings.Consumers.MarginTradingEnabledChanged, false,
                m =>
                {
                    _marginTradingSettingsCacheService.OnMarginTradingEnabledChanged(m);
                    return Task.CompletedTask;
                }, _rabbitMqService.GetJsonDeserializer<MarginTradingEnabledChangedMessage>());
        }
    }
}