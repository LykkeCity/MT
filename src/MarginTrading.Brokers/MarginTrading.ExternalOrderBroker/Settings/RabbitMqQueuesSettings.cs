// Copyright (c) 2019 Lykke Corp.

using MarginTrading.Common.RabbitMq;

namespace MarginTrading.ExternalOrderBroker.Settings
{
	public class RabbitMqQueuesSettings
	{
		public RabbitMqQueueInfo ExternalOrder { get; set; }
	}
}