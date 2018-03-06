using MarginTrading.Common.RabbitMq;

namespace MarginTrading.ExternalOrderBroker.Settings
{
	public class RabbitMqQueuesSettings
	{
		public RabbitMqQueueInfo ExternalOrder { get; set; }
	}
}