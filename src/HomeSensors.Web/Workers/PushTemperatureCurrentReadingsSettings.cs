namespace HomeSensors.Web.Workers;

public class PushTemperatureCurrentReadingsSettings
{
    public bool IsEnabled { get; set; } = true;
    public int BetweenTicksSeconds { get; set; } = 30;
}
