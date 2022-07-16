namespace HomeSensors.Web.Models;

public class GraphRequest
{
    public int IntervalMinutes { get; init; } = 15;
    public DateTimeOffset StartTime { get; init; } = DateTimeOffset.Now.AddHours(-48);
    public DateTimeOffset EndTime { get; init; } = DateTimeOffset.Now;
}
