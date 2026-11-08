namespace HomeSensors.Model.Notifications;

public class NoOpTemperatureHubNotifier : ITemperatureHubNotifier
{
    public Task NotifyCurrentReadingsChangedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
