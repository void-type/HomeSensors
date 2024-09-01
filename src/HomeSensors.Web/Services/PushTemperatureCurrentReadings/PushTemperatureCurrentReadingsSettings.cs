namespace HomeSensors.Web.Services.PushTemperatureCurrentReadings;

public class PushTemperatureCurrentReadingsSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksSeconds { get; init; } = 30;
}
