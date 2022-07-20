namespace HomeSensors.Data.Repositories.Models;

public class GraphTimeSeriesRequest
{
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now.AddHours(-48);
    public DateTimeOffset EndTime { get; init; } = DateTimeOffset.Now;
}
