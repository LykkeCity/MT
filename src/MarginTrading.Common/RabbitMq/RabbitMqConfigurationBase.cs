// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using MarginTrading.Common.Services;

namespace MarginTrading.Common.RabbitMq
{
    /// <summary>
    /// The base class for RabbitMq configuration (consumers and publishers)
    /// </summary>
    public abstract class RabbitMqConfigurationBase
    {
        [Optional]
        [AmqpCheck]
        public string ConnectionString { get; set; }
        
        public string ExchangeName { get; set; }
        
        [Optional]
        public bool IsDurable { get; set; } = true;
    }
    
    /// <summary>
    /// RabbitMq consumer configuration
    /// </summary>
    [UsedImplicitly]
    public sealed class RabbitMqConsumerConfiguration : RabbitMqConfigurationBase
    {
        [Optional]
        public int ConsumerCount { get; set; } = 1;

        [Optional]
        public string RoutingKey { get; set; }
    }
    
    /// <summary>
    /// RabbitMq publisher configuration
    /// </summary>
    public class RabbitMqPublisherConfiguration : RabbitMqConfigurationBase
    {
    }
    
    /// <summary>
    /// RabbitMq publisher configuration with logging
    /// </summary>
    public class RabbitMqPublisherConfigurationWithLogging : RabbitMqPublisherConfiguration
    {
        [Optional]
        public bool LogEventPublishing { get; set; } = true;

        public virtual IRabbitMqPublisherLoggingStrategy LoggingStrategy { get; } = new AlwaysOnLoggingStrategy();
    }
    
    /// <summary>
    /// RabbitMq publisher configuration with occasional logging
    /// </summary>
    [UsedImplicitly]
    public sealed class RabbitMqPublisherConfigurationWithOccasionalLogging : RabbitMqPublisherConfigurationWithLogging
    {
        public override IRabbitMqPublisherLoggingStrategy LoggingStrategy { get; } =
            new OneInAThousandLoggingStrategy();
    }
}