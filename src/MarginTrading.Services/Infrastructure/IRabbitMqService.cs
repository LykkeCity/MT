using System;
using System.Threading.Tasks;
using Common;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services.Infrastructure
{
    public interface IRabbitMqService
    {
        IMessageProducer<TMessage> CreateProducer<TMessage>(RabbitMqSettings settings);
        void Subscribe<TMessage>(RabbitMqSettings settings, string env, Func<TMessage, Task> handler);
    }
}
