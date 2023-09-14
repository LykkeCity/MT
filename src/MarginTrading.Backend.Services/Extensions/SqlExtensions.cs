// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarginTrading.Backend.Services.Extensions
{
    internal static class SqlExtensions
    {
        public static string ToBulkSqlStatement(
            this IDictionary<PerformanceTracker.MethodIdentity, PerformanceTracker.MethodStatistics> stats)
        {
            var sql = new StringBuilder();
            foreach (var chunk in stats.Chunk(1000))
            {
                sql.AppendLine(
                    $"INSERT INTO [dbo].[PerformanceStatistics] ([Owner], [Method], [Param], [CallsCounter], [TotalExecutionMs], [AverageExecutionMs], [MaxExecutionMs]) VALUES");
                
                var values = new StringBuilder();
                foreach (var (key, value) in chunk)
                {
                    if (values.Length > 0)
                        values.Append(",");
                    var line =
                        $"('{key.Owner}', '{key.Name}', '{key.Parameter}', {value.CallsCounter}, {value.TotalExecutionMs}, {value.TotalExecutionMs / value.CallsCounter}, {value.MaxExecutionMs})";
                    values.AppendLine(line);
                }

                values.Append(";");
                values.AppendLine();
                sql.Append(values);
            }
            
            return sql.ToString();
        }
    }
}