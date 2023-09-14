// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.Extensions
{
    internal static class SqlExtensions
    {
        public static string ToSqlStatement(this PerformanceTracker.MethodStatistics stat, 
            PerformanceTracker.MethodIdentity methodIdentity)
        {
            return $"INSERT INTO [dbo].[PerformanceStatistics] ([Owner], [Method], [Param], [CallsCounter], [TotalExecutionMs], [AverageExecutionMs], [MaxExecutionMs]) VALUES ('{methodIdentity.Owner}', '{methodIdentity.Name}', '{methodIdentity.Parameter}', {stat.CallsCounter}, {stat.TotalExecutionMs}, {stat.TotalExecutionMs / stat.CallsCounter}, {stat.MaxExecutionMs});";
        }
    }
}