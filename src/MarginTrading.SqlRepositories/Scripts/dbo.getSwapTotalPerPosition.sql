-- Copyright (c) 2019 BNP Paribas Arbitrage. All rights reserved.

CREATE OR ALTER PROCEDURE [dbo].[getSwapTotalPerPosition] (
	@positions AS [dbo].[PositionListDataType] READONLY
)
AS
BEGIN
    SELECT SUM(db_tb.ChangeAmount) as SwapTotal, db_tb.EventSourceId as PositionId
    FROM AccountHistory as db_tb
    INNER JOIN @positions AS param_tb
    ON db_tb.EventSourceId = param_tb.Id
    WHERE db_tb.ReasonType = 'Swap'
    GROUP BY db_tb.EventSourceId
END