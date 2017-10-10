using System;
using MarginTrading.Core.Monitoring;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Monitoring
{
    public class MonitoringRecordEntity : TableEntity, IMonitoringRecord
    {
        public static string GeneratePartitionKey()
        {
            return "Monitoring";
        }

        public static string GenerateRowKey(string serviceName)
        {
            return serviceName;
        }

        public static MonitoringRecordEntity Create(IMonitoringRecord record)
        {
            return new MonitoringRecordEntity
            {
                RowKey = GenerateRowKey(record.ServiceName),
                DateTime = record.DateTime,
                PartitionKey = GeneratePartitionKey(),
                Version = record.Version
            };
        }

        public string ServiceName => RowKey;
        public DateTime DateTime { get; set; }
        public string Version { get; set; }
    }
}
