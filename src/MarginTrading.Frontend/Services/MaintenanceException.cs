using System;

namespace MarginTrading.Frontend.Services
{
    public class MaintenanceException : Exception
    {
        public DateTime EnabledAt { get; }

        public MaintenanceException(DateTime enabledAt)
        {
            EnabledAt = enabledAt;
        }
    }
}