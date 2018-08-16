using System;
using MarginTrading.Common.Services;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.SqlRepositories.Entities
{
    public class OperationLogEntity : TableEntity, IOperationLog
    {
        public DateTime DateTime { get; set; }
        public string Name { get; set; }
        public string AccountId { get; set; }
        public string Input { get; set; }
        public string Data { get; set; }

        public static string GeneratePartitionKey(string accountId, string name)
        {
            return accountId ?? name;
        }

        public static OperationLogEntity Create(IOperationLog src, DateTime time)
        {
            return new OperationLogEntity
            {
                PartitionKey = GeneratePartitionKey(src.AccountId, src.Name),
                DateTime = time,
                Name = src.Name,
                Input = src.Input,
                Data = src.Data,
                AccountId = src.AccountId
            };
        } 
    }
}