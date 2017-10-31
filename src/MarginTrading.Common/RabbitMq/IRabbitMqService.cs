using System;
using System.Threading.Tasks;
using Common;

namespace MarginTrading.Common.RabbitMq
{
    public interface IRabbitMqService
    {
        IMessageProducer<TMessage> CreateProducer<TMessage>(RabbitMqSettings settings);
        void Subscribe<TMessage>(RabbitMqSettings settings, string env, Func<TMessage, Task> handler);
    }
}
