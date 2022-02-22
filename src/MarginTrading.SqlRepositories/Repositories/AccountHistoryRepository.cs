// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Model;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.SqlRepositories.Entities;
using Microsoft.Data.SqlClient;

namespace MarginTrading.SqlRepositories.Repositories;

public class AccountHistoryRepository: SqlRepositoryBase, IAccountHistoryRepository
{
    private readonly StoredProcedure _getSwapTotalPerPosition = new StoredProcedure("getSwapTotalPerPosition", "dbo");

    public AccountHistoryRepository(string connectionString): base(connectionString)
    {
        ExecCreateOrAlter(_getSwapTotalPerPosition.FileName);
    }

    public async Task<Dictionary<string, decimal>> GetSwapTotalPerPosition(IEnumerable<string> positionIds)
    {
        if (!positionIds.Any())
        {
            return new Dictionary<string, decimal>();
        }

        var positionIdCollection = new PositionIdCollection();
        positionIdCollection.AddRange(positionIds.Select(id => new PositionId {Id = id}));
            
        var items = await GetAllAsync(
            _getSwapTotalPerPosition.FullyQualifiedName,
            new[]
            {
                new SqlParameter
                {
                    ParameterName = "@positions",
                    SqlDbType = SqlDbType.Structured,
                    TypeName = "dbo.PositionListDataType",
                    Value = positionIdCollection
                }
            }, reader => new
            {
                PositionId = reader["PositionId"] as string,
                SwapTotal = Convert.ToDecimal(reader["SwapTotal"])
            });
        return items.ToDictionary(x => x.PositionId, x => x.SwapTotal);
    }
}