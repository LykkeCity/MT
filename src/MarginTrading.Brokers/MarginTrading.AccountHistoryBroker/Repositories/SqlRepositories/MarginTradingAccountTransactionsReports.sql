CREATE TABLE [MarginTradingAccountTransactionsReports](
	[Id] [nvarchar](64) NOT NULL,
	[Date] [datetime] NOT NULL,
	[ClientId] [nvarchar](64) NOT NULL,
	[AccountId] [nvarchar](64) NOT NULL,
	[PositionId] [text] NULL,
	[Amount] [numeric](18, 6) NOT NULL,
	[Balance] [numeric](18, 6) NOT NULL,
	[Type] [nvarchar](64) NOT NULL,
	[Comment] [text] NOT NULL,
	[WithdrawTransferLimit] [numeric](18, 6) NOT NULL,
 CONSTRAINT [PK_MarginTradingAccountTransactionsReports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
));