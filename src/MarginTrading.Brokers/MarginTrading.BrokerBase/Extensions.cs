using Dapper;
using System;
using System.Data;
using System.Data.SqlClient;

namespace MarginTrading.BrokerBase
{
    public static class Extensions
    {
        private const int DecimalPlaces = 10;
        private const decimal MaxSqlNumeric = 9999999999999999999999.999998m;
        private const decimal MinSqlNumeric = -9999999999999999999999.999998m;

        public static void CreateTableIfDoesntExists(this IDbConnection connection, string createQuery, string tableName)
        {
            try
            {
                connection.Open();
                try
                {
                    // Check if table exists
                    var res = connection.ExecuteScalar($"select top 1 * from {tableName}");
                }
                catch (SqlException)
                {
                    try
                    {
                        // Create table
                        string query = string.Format(createQuery, tableName);
                        connection.Query(query);
                    }
                    catch { throw; }
                }
                finally { connection.Close(); }
            }
            catch
            {
                throw;
            }
        }

        public static decimal ToRoundedDecimal(this double value)
        {
            return ValidateSqlNumeric(Convert.ToDecimal(value));
        }
        public static decimal? ToRoundedDecimal(this double? value)
        {
            if (value.HasValue)
                return ValidateSqlNumeric(Convert.ToDecimal(value.Value));
            else
                return null;
        }
        public static decimal ToRoundedDecimal(this decimal value)
        {
            return ValidateSqlNumeric(value);

        }
        public static decimal? ToRoundedDecimal(this decimal? value)
        {
            if (value.HasValue)
                return ValidateSqlNumeric(value.Value);
            else
                return null;
        }
        private static decimal ValidateSqlNumeric(decimal value)
        {
            var res = Math.Round(value, DecimalPlaces);
            if (res > MaxSqlNumeric)
                return MaxSqlNumeric;
            else if (res < MinSqlNumeric)
                return MinSqlNumeric;
            else
                return res;
        }
    }
}
