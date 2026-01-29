namespace HomeSensors.Model.Notifications;

public interface ITemperatureHubNotifier
{
    Task NotifyCurrentReadingsChangedAsync(CancellationToken cancellationToken = default);
}
