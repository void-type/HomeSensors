namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesRequest
{
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now.AddHours(-48);

    public DateTimeOffset EndTime { get; init; } = DateTimeOffset.Now;

    public List<long> LocationIds { get; init; } = [];
}
