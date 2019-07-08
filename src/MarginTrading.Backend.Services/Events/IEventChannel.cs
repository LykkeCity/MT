// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Services.Events
{
    public interface IEventChannel<TEventArgs>
    {
        void SendEvent(object sender, TEventArgs ea);
    }
}