// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Logs
{
    public class RiskSystemCommandsLogEntity : TableEntity
    {
        public string CommandType { get; set; }
        public string RawCommand { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; }

        public static string GeneratePartitionKey()
        {
            return "RiskSystemCommand";
        }
    }
}