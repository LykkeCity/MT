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
    public class PositionsHistoryRepository : IPositionsHistoryRepository
    {
        private readonly string _connectionString;
        private readonly string _select = @";WITH cte AS
       (
         SELECT *,
                ROW_NUMBER() OVER (PARTITION BY Id ORDER BY HistoryTimestamp DESC) AS rn
         FROM [{0}] ph
       )
SELECT *
FROM cte
WHERE rn = 1
  and cte.HistoryTimestamp > @Timestamp";

        public PositionsHistoryRepository(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _select = string.Format(_select, tableName);
        }
        
        public async Task<IReadOnlyList<IPositionHistory>> GetLastSnapshot(DateTime @from)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var data = await conn.QueryAsync<PositionHistoryEntity>(_select, new {Timestamp = @from});

                return data.Cast<IPositionHistory>().ToList();
            }
        }
    }
}