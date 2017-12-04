using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace MarginTrading.BrokerBase
{
    public static class Extensions
    {
        public static void CreateTableIfDoesntExists(this IDbConnection connection, string createQuery,
            string tableName)
        {
            connection.Open();
            try
            {
                // Check if table exists
                connection.ExecuteScalar($"select top 1 * from {tableName}");
            }
            catch (SqlException)
            {
                // Create table
                string query = string.Format(createQuery, tableName);
                connection.Query(query);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
