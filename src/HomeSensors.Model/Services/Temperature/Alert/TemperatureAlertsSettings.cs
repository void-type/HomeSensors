namespace HomeSensors.Model.Services.Temperature.Alert;

public class TemperatureAlertsSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksMinutes { get; init; } = 20;
    public int BetweenNotificationsMinutes { get; init; } = 120;
}
