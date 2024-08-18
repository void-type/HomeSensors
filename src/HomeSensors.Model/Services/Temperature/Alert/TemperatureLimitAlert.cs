using HomeSensors.Model.Repositories.Models;

namespace HomeSensors.Model.Services.Temperature.Alert;

public record TemperatureLimitAlert(TemperatureCheckLimitResponse Result, string Status, DateTimeOffset Expiry);
