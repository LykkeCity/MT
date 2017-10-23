using System.Threading.Tasks;
using MarginTrading.Core;
using Common.Log;
using System.Data;
using System.Data.SqlClient;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories
{
    internal class AccountMarginEventsReportsSqlRepository : IAccountMarginEventsReportsRepository
    {
        private const string TableName = "AccountMarginEventsReports";

        private readonly IDbConnection _connection;
        private readonly ILog _log;

        public AccountMarginEventsReportsSqlRepository(IMarginTradingSettingsService settings, ILog log)
        {
#if DEBUG
            _connection = new SqlConnection(@"Server=.\SQLEXPRESS;Database=StockExchange;User Id=sa;Password = na123456;");
            
#else
            _connection = new SqlConnection(settings.Db.ReportsConnString);
#endif
        }

        public Task InsertOrReplaceAsync(IAccountMarginEventReport report)
        {
            throw new System.NotImplementedException();
        }
    }
}
