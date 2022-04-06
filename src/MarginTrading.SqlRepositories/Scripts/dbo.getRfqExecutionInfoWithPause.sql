-- Copyright (c) 2022 BNP Paribas Arbitrage. All rights reserved.

CREATE OR ALTER PROCEDURE
    [dbo].[getRfqExecutionInfoWithPause](
    @id nvarchar(128) = NULL,
    @from datetime = NULL,
    @to datetime = NULL,
    @instrumentId nvarchar(128) = NULL,
    @accountId nvarchar(128) = NULL,
    @states as [dbo].[SpecialLiquidationStateListDataType] READONLY,
    @skip int = 0,
    @take int = 20)
AS
    -- =====================================================================================
-- Description:	
-- Returns the list of RFQs according to filters provided with extended information
-- on pause if any
-- =====================================================================================

BEGIN
    SET NOCOUNT ON;

    SELECT
        -- execution info
        i.Id                                            as Id,
        i.LastModified                                  as LastModified,
        i.OperationName                                 as OperationName,
        i.Data                                          as Data,
        -- current pause details 
        currentPause.Oid                                as currentPauseOid,
        currentPause.OperationId                        as currentPauseOperationId,
        currentPause.Source                             as currentPauseSource,
        currentPause.CreatedAt                          as currentPauseCreatedAt,
        currentPause.EffectiveSince                     as currentPauseEffectiveSince,
        currentPause.State                              as currentPauseState,
        currentPause.Initiator                          as currentPauseInitiator,
        -- latest cancelled pause details
        latestCancelledPause.Oid                        as latestCancelledPauseOid,
        latestCancelledPause.OperationId                as latestCancelledPauseOperationId,
        latestCancelledPause.Source                     as latestCancelledPauseSource,
        latestCancelledPause.CancellationSource         as latestCancelledPauseCancellationSource,
        latestCancelledPause.CreatedAt                  as latestCancelledPauseCreatedAt,
        latestCancelledPause.EffectiveSince             as latestCancelledPauseEffectiveSince,
        latestCancelledPause.State                      as latestCancelledPauseState,
        latestCancelledPause.Initiator                  as latestCancelledPauseInitiator,
        latestCancelledPause.CancelledAt                as latestCancelledPauseCancelledAt,
        latestCancelledPause.CancellationEffectiveSince as latestCancelledPauseCancellationEffectiveSince,
        latestCancelledPause.CancellationInitiator      as latestCancelledPauseCancellationInitiator,
        -- technical information
        count(*) OVER ()                                as TotalCount
    FROM [dbo].[MarginTradingExecutionInfo] i
             LEFT JOIN [dbo].[MarginTradingExecutionPause] currentPause ON (
                currentPause.OperationId = i.Id AND
                currentPause.OperationName = i.OperationName AND
                currentPause.State != 'Cancelled')
             LEFT JOIN [dbo].[MarginTradingExecutionPause] latestCancelledPause ON (
            latestCancelledPause.Oid = (
            SELECT MAX(Oid)
            FROM [dbo].[MarginTradingExecutionPause]
            WHERE OperationId = i.Id
              AND OperationName = i.OperationName
              AND [State] = 'Cancelled'
        ))
    WHERE i.OperationName = 'SpecialLiquidation'
      AND ((NULLIF(RTRIM(LTRIM(@id)), '') is NULL) OR (i.Id = RTRIM(LTRIM(@id))))
      AND ((@from is NULL) OR (i.LastModified >= @from))
      AND ((@to is NULL) OR (i.LastModified < @to))
      AND ((NULLIF(RTRIM(LTRIM(@instrumentId)), '') is NULL) OR (JSON_VALUE(i.Data, '$.Instrument') = RTRIM(LTRIM(@instrumentId))))
      AND ((NULLIF(RTRIM(LTRIM(@accountId)), '') is NULL) OR (JSON_VALUE(i.Data, '$.AccountId') = RTRIM(LTRIM(@accountId))))
      AND ((NOT EXISTS(SELECT 1 FROM @states)) OR (JSON_VALUE(i.Data, '$.State') in (SELECT Name FROM @states)))
    ORDER BY i.LastModified DESC
    OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
END
