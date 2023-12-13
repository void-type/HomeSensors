namespace HomeSensors.Model.Workers;

public class MqttTemperaturesSettings
{
    public bool IsEnabled { get; set; } = true;
    public string[] Topics { get; set; } = [];
}
