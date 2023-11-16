namespace HomeSensors.Model.Workers;

public class WorkersSettings
{
    public bool AlertsEnabled { get; set; } = true;
    public int AlertsBetweenTicksMinutes { get; set; } = 20;
    public int AlertsBetweenNotificationsMinutes { get; set; } = 120;

    public bool MqttTemperaturesEnabled { get; set; } = true;
    public string[] MqttTemperaturesTopics { get; set; } = [];

    public bool PushTemperatureCurrentReadingsEnabled { get; set; } = true;
    public int PushTemperatureCurrentReadingsBetweenTicksSeconds { get; set; } = 30;

    public bool SummarizeTemperatureReadingsEnabled { get; set; } = true;
    public int SummarizeTemperatureReadingsBetweenTicksMinutes { get; set; } = 60;
    public int SummarizeTemperatureReadingsSummarizeCutoffDays { get; set; } = 30;
    public int SummarizeTemperatureReadingsChunkSize { get; set; } = 5;
    public int SummarizeTemperatureReadingsDelayFirstTickMinutes { get; set; } = 5;
}
