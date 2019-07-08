// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <summary>
    /// Sends cqrs messages from Trading Engine contexts
    /// </summary>
    public interface ICqrsSender
    {
        void SendCommandToAccountManagement<T>(T command);
        void SendCommandToSettingsService<T>(T command);
        void SendCommandToSelf<T>(T command);
        void PublishEvent<T>(T ev, string boundedContext = null);
    }
}