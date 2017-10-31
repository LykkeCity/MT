using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common;
using Common.Log;

namespace MarginTrading.Backend.Services.Events
{
    public class EventChannel<TEventArgs> : IEventChannel<TEventArgs>, IDisposable
    {
        private IComponentContext _container;
        private readonly ILog _log;
        private IEventConsumer<TEventArgs>[] _consumers;
        private readonly object _sync = new object();

        public EventChannel(IComponentContext container, ILog log)
        {
            _container = container;
            _log = log;
        }

        public void SendEvent(object sender, TEventArgs ea)
        {
            AssertInitialized();
            foreach (var consumer in _consumers)
            {
                try
                {
                    consumer.ConsumeEvent(sender, ea);
                }
                catch (Exception e)
                {
                    _log.WriteErrorAsync($"Event chanel {typeof(TEventArgs).Name}", "SendEvent", ea.ToJson(), e);
                }
            }
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
