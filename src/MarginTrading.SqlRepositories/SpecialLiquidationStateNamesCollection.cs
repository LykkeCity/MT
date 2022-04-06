// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient.Server;

namespace MarginTrading.SqlRepositories
{
    public class SpecialLiquidationStateNamesCollection : List<StateName>, IEnumerable<SqlDataRecord>
    {
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
        {
            var sqlRow = new SqlDataRecord(
                new SqlMetaData(nameof(StateName.Name), SqlDbType.NVarChar, 64));

            foreach (var stateName in this)
            {
                sqlRow.SetString(0, stateName.Name);

                yield return sqlRow;
            }
        }
    }
}