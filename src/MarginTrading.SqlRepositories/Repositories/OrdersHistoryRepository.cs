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
        private readonly string _select = @";WITH cte AS
       (
         SELECT *,
                ROW_NUMBER() OVER (PARTITION BY Id ORDER BY MaxDate DESC) AS rn
         FROM [{0}] oh
                CROSS APPLY (SELECT MAX(CombinedDate)
                             FROM (VALUES (oh.CreatedTimestamp),
                                          (oh.ModifiedTimestamp),
                                          (oh.ActivatedTimestamp),
                                          (oh.ExecutionStartedTimestamp),
                                          (oh.ExecutedTimestamp),
                                          (oh.CanceledTimestamp)) AS x (CombinedDate)) AS x (MaxDate)
       )
SELECT *
FROM cte
WHERE rn = 1
  AND cte.MaxDate > @Timestamp";

        public OrdersHistoryRepository(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _select = string.Format(_select, tableName);
        }
        
        public async Task<IReadOnlyList<IOrderHistory>> GetLastSnapshot(DateTime @from)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var data = await conn.QueryAsync<OrderHistoryEntity>(_select, new {Timestamp = @from});

                return data.Cast<IOrderHistory>().ToList();
            }
        }
    }
}