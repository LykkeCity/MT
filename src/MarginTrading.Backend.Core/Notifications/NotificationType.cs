// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Backend.Core.Notifications
{
    public enum NotificationType
    {
        PositionOpened = 9,
        PositionClosed = 10,
        MarginCall = 11
    }

    public static class EventsAndEntities
    {
        // ReSharper disable once InconsistentNaming
        public const string MarginWallet = "MarginWallet";

        public const string PositionOpened = "PositionOpened";
        public const string PositionClosed = "PositionClosed";
        public const string MarginCall = "MarginCall";

        public static string GetEntity(NotificationType notification)
        {
            switch (notification)
            {
                case NotificationType.PositionOpened:
                case NotificationType.PositionClosed:
                case NotificationType.MarginCall:
                    return MarginWallet;
                default:
                    throw new ArgumentException("Unknown notification");
            }
        }

        public static string GetEvent(NotificationType notification)
        {
            switch (notification)
            {
                case NotificationType.PositionOpened:
                    return PositionOpened;
                case NotificationType.PositionClosed:
                    return PositionClosed;
                case NotificationType.MarginCall:
                    return MarginCall;
                default:
                    throw new ArgumentException("Unknown notification");
            }
        }
    }
}
