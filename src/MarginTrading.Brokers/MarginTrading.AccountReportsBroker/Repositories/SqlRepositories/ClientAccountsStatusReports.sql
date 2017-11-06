CREATE TABLE [ClientAccountsStatusReports](
	[Id] [nvarchar](64) NOT NULL,
	[Date] [datetime] NOT NULL,
	[BaseAssetId] [nvarchar](64) NOT NULL,
	[AccountId] [nvarchar](64) NOT NULL,
	[ClientId] [nvarchar](64) NOT NULL,
	[TradingConditionId] [nvarchar](64) NOT NULL,
	[Balance] [numeric](20, 10) NOT NULL,
	[WithdrawTransferLimit] [numeric](20, 10) NOT NULL,
	[MarginCall] [numeric](20, 10) NOT NULL,
	[StopOut] [numeric](20, 10) NOT NULL,
	[TotalCapital] [numeric](20, 10) NOT NULL,
	[FreeMargin] [numeric](20, 10) NOT NULL,
	[MarginAvailable] [numeric](20, 10) NOT NULL,
	[UsedMargin] [numeric](20, 10) NOT NULL,
	[MarginInit] [numeric](20, 10) NOT NULL,
	[PnL] [numeric](20, 10) NOT NULL,
	[OpenPositionsCount] [numeric](20, 10) NOT NULL,
	[MarginUsageLevel] [numeric](20, 10) NOT NULL,
	[IsLive] [bit] NOT NULL,
 CONSTRAINT [PK_ClientAccountsStatusReports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
));