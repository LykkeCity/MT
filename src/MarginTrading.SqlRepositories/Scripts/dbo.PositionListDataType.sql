-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

USE [nova]
GO

-- =====================================================================================
-- Create PositionListDataType Type Table to be used by stored procedure for validations
-- =====================================================================================

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE [name] = 'PositionListDataType' AND schema_id = schema_id('dbo'))
BEGIN
CREATE TYPE [dbo].[PositionListDataType] AS TABLE
    (
        [Id] nvarchar(64) NOT NULL
    )
END
GO