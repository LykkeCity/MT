﻿using Common.Log;
using Dapper;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.Repositories.SqlRepositories
{
    internal class AccountTransactionsReportsSqlRepository : IAccountTransactionsReportsRepository
    {
        private const string TableName = "MarginTradingAccountTransactionsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[Date] [datetime] NOT NULL," +
            "[ClientId] [nvarchar] (64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[PositionId] [text] NULL, " +
            "[Amount] [numeric] (20, 10) NOT NULL, " +
            "[Balance] [numeric] (20, 10) NOT NULL, " +
            "[Type] [nvarchar] (50) NOT NULL, " +
            "[Comment] [text] NOT NULL, " +
            "[WithdrawTransferLimit] [numeric] (20, 10) NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly IDbConnection _connection;
        private readonly ILog _log;

        public AccountTransactionsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _connection = new SqlConnection(settings.Db.ReportsSqlConnString);
            _connection.CreateTableIfDoesntExists(CreateTableScript, TableName);
        }

        public async Task InsertOrReplaceAsync(IAccountTransactionsReport entity)
        {
            string query = $"insert into {TableName} " +
                "(Id, Date, AccountId, ClientId, Amount, Balance, WithdrawTransferLimit, Comment, Type) " +
                " values " +
                "(@Id ,@Date, @AccountId, @ClientId, @Amount, @Balance, @WithdrawTransferLimit, @Comment, @Type)";

            await _connection.ExecuteAsync(query, entity);
        }
    }
}
