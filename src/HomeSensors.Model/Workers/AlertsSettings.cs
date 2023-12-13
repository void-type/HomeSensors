namespace HomeSensors.Model.Workers;

public class AlertsSettings
{
    public bool IsEnabled { get; set; } = true;
    public int BetweenTicksMinutes { get; set; } = 20;
    public int BetweenNotificationsMinutes { get; set; } = 120;
}
