using System;
using MarginTrading.Frontend.Repositories.Contract;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.Frontend.Repositories.Entities
{
    public class MaintenanceInfoEntity : TableEntity, IMaintenanceInfo
    {
        public static string GetPartitionKey()
        {
            return "MaintenanceInfo";
        }

        public static string GetDemoRowKey()
        {
            return "Demo";
        }
        
        public static string GetLiveRowKey()
        {
            return "Live";
        }
        
        public bool IsEnabled { get; set; }
        public DateTime ChangedDate { get; set; }
        public string ChangedReason { get; set; }
        public string ChangedBy { get; set; }
    }
}