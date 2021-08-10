// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.SqlRepositories.Entities;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class OrdersHistoryRepository : IOrdersHistoryRepository
    {
        private readonly string _connectionString;
        private readonly int _getLastSnapshotTimeoutS;

        private readonly string _select = @"
;WITH 
    filteredOrderHist AS (
        SELECT *,
               CASE [Status]
                   WHEN 'Placed' THEN 0
                   WHEN 'Inactive'THEN 1
                   WHEN 'Active'THEN 2
                   WHEN 'ExecutionStarted'THEN 3
                   WHEN 'Executed'THEN 4
                   WHEN 'Canceled'THEN 5
                   WHEN 'Rejected'THEN 6
                   WHEN 'Expired'THEN 7
                   ELSE 99
               END as StatusOrder
        FROM [{0}] oh (NOLOCK)
        WHERE oh.ModifiedTimestamp > @Timestamp
    ),
    filteredOrderHistWithRowNumber AS (
        SELECT *, ROW_NUMBER() OVER (PARTITION BY Id ORDER BY
            [ModifiedTimestamp] DESC,
            StatusOrder DESC
            ) AS RowNumber
        FROM filteredOrderHist
    )
SELECT *
FROM filteredOrderHistWithRowNumber
WHERE RowNumber = 1";

        public OrdersHistoryRepository(string connectionString, string tableName, int getLastSnapshotTimeoutS)
        {
            _connectionString = connectionString;
            _select = string.Format(_select, tableName);
            _getLastSnapshotTimeoutS = getLastSnapshotTimeoutS;
        }

        public async Task<IReadOnlyList<IOrderHistory>> GetLastSnapshot(DateTime @from)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var data = await conn.QueryAsync<OrderHistoryEntity>(_select, new { Timestamp = @from }, commandTimeout: _getLastSnapshotTimeoutS);

                return data.Cast<IOrderHistory>().ToList();
            }
        }
    }
}