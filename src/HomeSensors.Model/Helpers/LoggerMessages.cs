using Microsoft.Extensions.Logging;

namespace HomeSensors.Model.Helpers;

public static partial class LoggerMessages
{
    [LoggerMessage(0, LogLevel.Information, "{Payload}")]
    public static partial void LogMqttPayload(ILogger logger, string payload);
}
