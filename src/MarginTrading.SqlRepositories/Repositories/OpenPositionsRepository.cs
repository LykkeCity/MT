using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class OpenPositionsRepository : IOpenPositionsRepository
    {
        private const string TableName = "OpenPositionsDump";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 @"[OID] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY,
[Id] [nvarchar](64) NOT NULL,
[DealId] [nvarchar](128) NULL,
[Code] [bigint] NULL,
[AssetPairId] [nvarchar] (64) NULL,
[Direction] [nvarchar] (64) NULL,
[Volume] [float] NULL,
[AccountId] [nvarchar] (64) NULL,
[TradingConditionId] [nvarchar] (64) NULL,
[AccountAssetId] [nvarchar] (64) NULL,
[ExpectedOpenPrice] [float] NULL,
[OpenMatchingEngineId] [nvarchar] (64) NULL,
[OpenDate] [datetime] NULL,
[OpenTradeId] [nvarchar] (64) NULL,
[OpenPrice] [float] NULL,
[OpenFxPrice] [float] NULL,
[EquivalentAsset] [nvarchar] (64) NULL,
[OpenPriceEquivalent] [float] NULL,
[RelatedOrders] [nvarchar](1024) NULL,
[LegalEntity] [nvarchar] (64) NULL,
[OpenOriginator] [nvarchar] (64) NULL,
[ExternalProviderId] [nvarchar] (64) NULL,
[SwapCommissionRate] [float] NULL,
[OpenCommissionRate] [float] NULL,
[CloseCommissionRate] [float] NULL,
[CommissionLot] [float] NULL,
[CloseMatchingEngineId] [nvarchar] (64) NULL,
[ClosePrice] [float] NULL,
[CloseFxPrice] [float] NULL,
[ClosePriceEquivalent] [float] NULL,
[StartClosingDate] [datetime] NULL,
[CloseDate] [datetime] NULL,
[CloseOriginator] [nvarchar] (64) NULL,
[CloseReason] [nvarchar] (256) NULL,
[CloseComment] [nvarchar] (256) NULL,
[CloseTrades] [nvarchar] (1024) NULL,
[LastModified] [datetime] NULL,
[TotalPnL] [float] NULL,
[ChargedPnl] [float] NULL,
[HistoryType] [nvarchar] (64) NULL,
[DealInfo] [nvarchar] (1024) NULL,
[HistoryTimestamp] [datetime] NULL
);";
        
        private static Type DataType => typeof(PositionContract);//todo use entity/interface
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));

        private readonly IConvertService _convertService;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;

        /// <summary>
        /// For testing purposes
        /// </summary>
        public OpenPositionsRepository(){}
        
        public OpenPositionsRepository(IConvertService convertService, MarginTradingSettings settings, ILog log)
        {
            _convertService = convertService;
            _log = log;
            _settings = settings;
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountMarginFreezingRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task Dump(IEnumerable<PositionContract> openPositions)
        {
            
        }
    }
}