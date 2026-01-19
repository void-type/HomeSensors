namespace HomeSensors.Model.Temperature.Models;

public record TemperatureLimitAlert(TemperatureCheckLimitResponse Result, string Status, DateTimeOffset Expiry);
