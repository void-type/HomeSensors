namespace HomeSensors.Model.Notifications;

public class NoOpTemperatureHubNotifier : ITemperatureHubNotifier
{
#pragma warning disable AsyncAwaitAnalyzer // Method should use async/await
    public Task NotifyCurrentReadingsChangedAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
#pragma warning restore AsyncAwaitAnalyzer
}
