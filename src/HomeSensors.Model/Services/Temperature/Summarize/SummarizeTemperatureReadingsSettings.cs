namespace HomeSensors.Model.Services.Temperature.Summarize;

public class SummarizeTemperatureReadingsSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksMinutes { get; init; } = 20;
    public int SummarizeCutoffDays { get; init; } = 30;
    public int ChunkSize { get; init; } = 5;
    public int DelayFirstTickMinutes { get; init; } = 5;
}
