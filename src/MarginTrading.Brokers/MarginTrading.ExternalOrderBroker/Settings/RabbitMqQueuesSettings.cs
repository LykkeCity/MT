// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Common.RabbitMq;

namespace MarginTrading.ExternalOrderBroker.Settings
{
	public class RabbitMqQueuesSettings
	{
		public RabbitMqQueueInfo ExternalOrder { get; set; }
	}
}