-- Create a CTE to identify duplicate records caused by deadlocks.
WITH DuplicateCTE AS (
  SELECT
    [Time],
    TemperatureDeviceId,
    TemperatureLocationId,
    IsSummary,
    ROW_NUMBER() OVER (PARTITION BY Time, TemperatureDeviceId, TemperatureLocationId, IsSummary ORDER BY [Id]) AS RowNum
  FROM TemperatureReadings
)

-- Delete duplicates, keeping the first occurrence
-- SELECT COUNT(1)
DELETE
FROM DuplicateCTE
WHERE RowNum > 1 AND IsSummary = 1;
