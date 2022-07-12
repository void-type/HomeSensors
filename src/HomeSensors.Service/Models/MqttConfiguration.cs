namespace HomeSensors.Service.Models;

public class MqttConfiguration
{
    public string Server { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string[] Topics { get; set; } = Array.Empty<string>();
    public bool LogMessages { get; set; }
}
