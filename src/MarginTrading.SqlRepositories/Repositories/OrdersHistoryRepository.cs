// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        private readonly string _tableName;
        private readonly int _getLastSnapshotTimeoutS;

        public OrdersHistoryRepository(string connectionString, string tableName, int getLastSnapshotTimeoutS)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _getLastSnapshotTimeoutS = getLastSnapshotTimeoutS;
        }

        public async Task<IReadOnlyList<IOrderHistory>> GetLastSnapshot(DateTime @from)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var idsToSelectColumns = new string[] {
                    nameof(OrderHistoryEntity.OID),
                    nameof(OrderHistoryEntity.Id),
                    nameof(OrderHistoryEntity.ModifiedTimestamp)
                };
                var idsToSelect = (await conn.QueryAsync<OrderHistoryEntity>(
                    $"SELECT {string.Join(",", idsToSelectColumns)} FROM {_tableName} WHERE ModifiedTimestamp > @Timestamp",
                    new { Timestamp = @from },
                    commandTimeout: _getLastSnapshotTimeoutS))
                    .OrderByDescending(x => x.ModifiedTimestamp)
                    .GroupBy(x => x.Id)
                    .Select(g => g.First().OID)
                    .ToList();

                return (await conn.QueryAsync<OrderHistoryEntity>(
                    $"SELECT * FROM {_tableName} WHERE {nameof(OrderHistoryEntity.OID)} IN @IdsToSelect",
                    new { IdsToSelect = idsToSelect },
                    commandTimeout: _getLastSnapshotTimeoutS))
                    .Cast<IOrderHistory>()
                    .ToList();
            }
        }
    }
}