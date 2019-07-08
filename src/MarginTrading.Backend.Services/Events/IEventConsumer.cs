// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.Events
{
    public interface IEventConsumer
    {
        /// <summary>
        /// Less ConsumerRank are called first
        /// </summary>
        int ConsumerRank { get; }
    }

    public interface IEventConsumer<in TEventArgs> : IEventConsumer
    {
        void ConsumeEvent(object sender, TEventArgs ea);
    }
}