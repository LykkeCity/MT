-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

-- =====================================================================================
-- Create SpecialLiquidationStateListDataType Type Table to be used by stored procedure for filtering
-- =====================================================================================

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE [name] = 'SpecialLiquidationStateListDataType' AND schema_id = schema_id('dbo'))
BEGIN
CREATE TYPE [dbo].[SpecialLiquidationStateListDataType] AS TABLE
    (
        [Name] nvarchar(64) NOT NULL
    )
END