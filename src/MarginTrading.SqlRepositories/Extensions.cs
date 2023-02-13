// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Data;
using Dapper;

namespace MarginTrading.SqlRepositories
{
    public static class Extensions
    {
        private static string CreateIfNotExistsScriptFmt =
            @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}]') AND type in (N'U'))
BEGIN
    {1}
END";
        
        public static void CreateTableIfDoesntExists(this IDbConnection connection, string createSqlFmt,
            string tableName)
        {
            var createTableScript = string.Format(createSqlFmt, tableName);
            
            var createIfNotExistsScript = string.Format(CreateIfNotExistsScriptFmt, tableName, createTableScript);
            
            connection.Open();
            try
            {
                connection.Execute(createIfNotExistsScript);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}