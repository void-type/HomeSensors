namespace HomeSensors.Model.Data.Models;

public class HvacAction
{
    public const string Heating = "heating";
    public const string Cooling = "cooling";

    public long Id { get; set; }

    public string EntityId { get; set; } = string.Empty;

    // Idle, fan, cooling, heating
    public string State { get; set; } = string.Empty;

    // Last Changed indicates the last time the state of an entity was modified.
    public DateTimeOffset LastChanged { get; set; }

    // Last Updated indicates the last time any change (state or attribute) was recorded for that entity.
    public DateTimeOffset LastUpdated { get; set; }
}
