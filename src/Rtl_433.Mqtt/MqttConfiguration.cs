namespace Rtl_433.Mqtt
{
    public class MqttConfiguration
    {
        public string Server { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
