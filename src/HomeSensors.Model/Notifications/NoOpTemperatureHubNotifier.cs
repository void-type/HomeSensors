namespace HomeSensors.Model.Notifications;

public class NoOpTemperatureHubNotifier : ITemperatureHubNotifier
{
#pragma warning disable AsyncAwaitAnalyzer // Method should use async/await
    public Task NotifyCurrentReadingsChangedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
#pragma warning restore AsyncAwaitAnalyzer
}
