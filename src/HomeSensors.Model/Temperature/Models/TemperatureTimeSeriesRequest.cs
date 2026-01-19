namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesRequest
{
    public required DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now.AddHours(-48);

    public required DateTimeOffset EndTime { get; init; } = DateTimeOffset.Now;

    public required List<long> LocationIds { get; init; } = [];

    public required bool IncludeHvacActions { get; init; }

    public required bool TrimHvacActionsToRequestedTimeRange { get; init; }
}
