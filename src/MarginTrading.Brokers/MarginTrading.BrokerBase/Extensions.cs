using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace MarginTrading.BrokerBase
{
    public static class Extensions
    {
        public static void CreateTableIfDoesntExists(this IDbConnection connection, string createQuery, string tableName)
        {
            try
            {
                connection.Open();
                try
                {
                    // Check if table exists
                    var res = connection.ExecuteScalar($"select top 1 Id from {tableName}");
                }
                catch (SqlException)
                {
                    try
                    {
                        // Create table
                        string query = string.Format(createQuery, tableName);
                        connection.QueryAsync(query);
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
    }
}
