CREATE TABLE [MarginTradingAccountTransactionsReports](
	[Id] [nvarchar](32) NOT NULL,
	[Date] [datetime] NOT NULL,
	[ClientId] [nvarchar](32) NOT NULL,
	[AccountId] [nvarchar](32) NOT NULL,
	[PositionId] [text] NOT NULL,
	[Amount] [numeric](18, 18) NOT NULL,
	[Balance] [numeric](18, 18) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Comment] [text] NOT NULL,
	[WithdrawTransferLimit] [numeric](18, 18) NOT NULL,
 CONSTRAINT [PK_Table_2] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
));