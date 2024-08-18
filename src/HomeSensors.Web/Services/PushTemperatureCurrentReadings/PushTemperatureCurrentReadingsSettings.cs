namespace HomeSensors.Web.Services.PushTemperatureCurrentReadings;

public class PushTemperatureCurrentReadingsSettings
{
    public bool IsEnabled { get; set; } = true;
    public int BetweenTicksSeconds { get; set; } = 30;
}
