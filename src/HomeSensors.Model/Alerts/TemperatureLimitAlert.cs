using HomeSensors.Model.Repositories.Models;

namespace HomeSensors.Model.Alerts;

public record TemperatureLimitAlert(TemperatureCheckLimitResponse Result, string Status, DateTimeOffset Expiry);
