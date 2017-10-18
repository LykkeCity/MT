using System;
using System.Threading.Tasks;
using Common;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.HelperServices
{
    public interface IRabbitMqService
    {
        IMessageProducer<TMessage> GetProducer<TMessage>(RabbitConnectionSettings settings, bool isDurable);
        void Subscribe<TMessage>(RabbitConnectionSettings settings, bool isDurable, Func<TMessage, Task> handler);
    }
}
