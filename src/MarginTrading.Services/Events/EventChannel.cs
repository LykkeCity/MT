using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace MarginTrading.Services.Events
{
    public class EventChannel<TEventArgs> : IEventChannel<TEventArgs>, IDisposable
    {
        private IComponentContext _container;
        private IEventConsumer<TEventArgs>[] _consumers;
        private readonly object _sync = new object();

        public EventChannel(IComponentContext container)
        {
            _container = container;
        }

        public void SendEvent(object sender, TEventArgs ea)
        {
            AssertInitialized();
            foreach (var consumer in _consumers)
                consumer.ConsumeEvent(sender, ea);
        }

        public void Dispose()
        {
            _consumers = null;
            _container = null;
        }

        public int AssertInitialized()
        {
            if (null != _consumers)
                return _consumers.Length;
            lock (_sync)
            {
                if (null != _consumers)
                    return _consumers.Length;

                if (null == _container)
                    throw new ObjectDisposedException(GetType().Name);

                _consumers =
                    Enumerable.OrderBy(_container.Resolve<IEnumerable<IEventConsumer<TEventArgs>>>(), x => x.ConsumerRank).ToArray();
                _container = null;
            }
            return _consumers.Length;
        }
    }
}
