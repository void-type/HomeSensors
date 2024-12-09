namespace HomeSensors.Model.Services.Temperature.Alert;

public class TemperatureAlertsSettings
{
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Minutes between each check.
    /// </summary>
    public int BetweenTicksMinutes { get; init; } = 20;

    /// <summary>
    /// The number of minutes of data to look back and evaluate for alerts.
    /// If greater than zero, data within the look back period will be averaged over the look back range; which can smooth out false alerts. If using a look back range, it is recommended to make this bigger than the time between ticks.
    /// If zero, then each data point since the last check will be evaluated individually for exceeding the threshold.
    /// </summary>
    public int LookBackMinutes { get; init; } = 30;

    /// <summary>
    /// The number of minutes between reminders for an ongoing alert.
    /// </summary>
    public int BetweenNotificationsMinutes { get; init; } = 120;
}
