USE [nova]
GO

ALTER TABLE [dbo].[TradingEngineSnapshots]
    ADD [Status] [nvarchar](32) NOT NULL
    CONSTRAINT [TradingEngineSnapshots_Status_Default_Value]
    DEFAULT ('Final')
GO