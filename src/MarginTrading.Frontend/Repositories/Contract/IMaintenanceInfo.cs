using System;

namespace MarginTrading.Frontend.Repositories.Contract
{
    public interface IMaintenanceInfo
    {
        bool IsEnabled { get; }
        DateTime ChangedDate { get; }
        string ChangedReason { get; }
        string ChangedBy { get; }
    }

    public class MaintenanceInfo : IMaintenanceInfo
    {
        public bool IsEnabled { get; set; }
        public DateTime ChangedDate { get; set; }
        public string ChangedReason { get; set; }
        public string ChangedBy { get; set; }
    }
}