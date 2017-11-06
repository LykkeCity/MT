CREATE TABLE [ClientAccountsReports](
	[Id] [nvarchar](64) NOT NULL,
	[Date] [datetime] NOT NULL,
	[TakerCounterpartyId] [nvarchar](64) NOT NULL,
	[TakerAccountId] [nvarchar](64) NOT NULL,
	[BaseAssetId] [nvarchar](64) NOT NULL,
	[IsLive] [bit] NOT NULL,
 CONSTRAINT [PK_ClientAccountsReports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
));