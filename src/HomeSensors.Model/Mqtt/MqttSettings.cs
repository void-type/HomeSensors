namespace HomeSensors.Model.Mqtt;

public class MqttSettings
{
    public string Server { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool LogMessages { get; set; }
}
