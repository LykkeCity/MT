using System;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IOvernightSwapNotificationService
    {
        void PerformEmailNotification(DateTime calculationTime);
    }
}