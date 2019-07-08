// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.Events
{
    public interface IEventChannel<TEventArgs>
    {
        void SendEvent(object sender, TEventArgs ea);
    }
}