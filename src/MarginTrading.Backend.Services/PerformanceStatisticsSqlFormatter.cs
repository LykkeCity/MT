// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Text;
using MarginTrading.Backend.Services.Extensions;

namespace MarginTrading.Backend.Services
{
    public static class PerformanceStatisticsSqlFormatter
    {
        public static string GenerateInsertions()
        {
            var sql = new StringBuilder();
            sql.AppendLine(Ddl);

            var bulkInsertions = PerformanceTracker.Statistics.ToBulkSqlStatement();
            return sql.AppendLine(bulkInsertions).ToString();
        }

        private static string Ddl =>
            $@"
use nova

if object_id('dbo.PerformanceStatistics', 'U') is not null
    truncate table [dbo].[PerformanceStatistics]
else
    CREATE TABLE dbo.PerformanceStatistics
    (
        Owner NVARCHAR(100) NOT NULL,
        Method NVARCHAR(100) NOT NULL,
        Param NVARCHAR(100) NOT NULL,
        CallsCounter INT NOT NULL,
        TotalExecutionMs BIGINT NOT NULL,
        AverageExecutionMs BIGINT NOT NULL,
        MaxExecutionMs BIGINT NOT NULL,
        PRIMARY KEY (Owner, Method, Param)
    )

";
    }
}