using Common.Log;
using MarginTrading.AccountHistoryBroker.Repositories.SqlRepositories;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MarginTradingTests.Unit.SqlConnection
{
    [TestFixture]
    [Ignore("LocalSqlDb")]
    public class SqlRepositories
    {
        string _dbName;
        MarginTrading.AccountHistoryBroker.Settings _accountHistoryBrokerSettings;
        ILog _log;
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _dbName = "UnitTests";
            
            _log = new LogToConsole();

            _accountHistoryBrokerSettings = new MarginTrading.AccountHistoryBroker.Settings
            {
                Db = new MarginTrading.AccountHistoryBroker.Db
                {
                    ReportsSqlConnString = $"Server=.\\SQLEXPRESS1;Database={_dbName};User Id=sa;Password = na123456;"
                }
            };
        }

        [Test]
        [Category("SqlConnection")]
        [Category("AccountHistoryBroker")]        
        public void AccountTransactionsReportsInsertAndUpdate()
        {
            var repo = new AccountTransactionsReportsSqlRepository(_accountHistoryBrokerSettings, _log);
            var domainObject = new MarginTrading.AccountHistoryBroker.Repositories.Models.AccountTransactionsReport
            {
                Id = "Id",
                Date = DateTime.UtcNow,
                AccountId = "AccountId",
                ClientId = "ClientId",
                PositionId= null,
                Comment = "Comment",
                Type = "Buy",
                Amount = -9999999999999999999999.9999989999,
                Balance = 9999999999999999999999.9999989999,
                WithdrawTransferLimit = 0
            };
            // Insert
            Task.Run(async () =>
            {
                await repo.InsertOrReplaceAsync(domainObject);
            }).Wait();
            // Update
            Task.Run(async () =>
            {
                await repo.InsertOrReplaceAsync(domainObject);
            }).Wait();
        }
        [Test]
        [Category("Error")]
        [Category("SqlConnection")]
        [Category("AccountHistoryBroker")]
        public void AccountTransactionsReportsInsertAndUpdate_ArithmeticException()
        {
            var repo = new AccountTransactionsReportsSqlRepository(_accountHistoryBrokerSettings, _log);
            var domainObject = new MarginTrading.AccountHistoryBroker.Repositories.Models.AccountTransactionsReport
            {
                Id = "Id",
                Date = DateTime.UtcNow,
                AccountId = "AccountId",
                ClientId = "ClientId",
                PositionId = null,
                Comment = "Comment",
                Type = "Buy",
                Amount = -9999999999999999999999.9999999999,
                Balance = 9999999999999999999999.1,
                WithdrawTransferLimit = 0
            };
            // Arithmetic Exception
            Assert.ThrowsAsync<System.Exception>(async () => await repo.InsertOrReplaceAsync(domainObject));
            
            
        }
    }
}
