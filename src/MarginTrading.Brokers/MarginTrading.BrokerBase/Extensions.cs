using Dapper;
using System;
using System.Data;
using System.Data.SqlClient;

namespace MarginTrading.BrokerBase
{
    public static class Extensions
    {
        private const int DecimalPlaces = 10;

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
            return Math.Round(Convert.ToDecimal(value), DecimalPlaces);
        }
        public static decimal? ToRoundedDecimal(this double? value)
        {
            if (value.HasValue)
                return Math.Round(Convert.ToDecimal(value.Value), DecimalPlaces);
            else
                return null;
        }
        public static decimal ToRoundedDecimal(this decimal value)
        {
            return Math.Round(value, DecimalPlaces);
        }
        public static decimal? ToRoundedDecimal(this decimal? value)
        {
            if (value.HasValue)
                return Math.Round(value.Value, DecimalPlaces);
            else
                return null;
        }
    }
}
