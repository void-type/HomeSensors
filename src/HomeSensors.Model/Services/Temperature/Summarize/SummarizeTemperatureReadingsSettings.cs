namespace HomeSensors.Model.Services.Temperature.Summarize;

public class SummarizeTemperatureReadingsSettings
{
    public bool IsEnabled { get; set; } = true;
    public int BetweenTicksMinutes { get; set; } = 20;
    public int SummarizeCutoffDays { get; set; } = 30;
    public int ChunkSize { get; set; } = 5;
    public int DelayFirstTickMinutes { get; set; } = 5;
}
