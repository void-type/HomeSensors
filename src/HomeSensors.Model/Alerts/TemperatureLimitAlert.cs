using HomeSensors.Model.Repositories.Models;

namespace HomeSensors.Model.Alerts;

public record TemperatureLimitAlert(CheckLimitResult Result, string Status, DateTimeOffset Expiry);
