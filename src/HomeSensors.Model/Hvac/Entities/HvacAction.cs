namespace HomeSensors.Model.Hvac.Entities;

public class HvacAction
{
    public const string Heating = "heating";
    public const string Cooling = "cooling";

    public long Id { get; set; }

    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Idle, fan, cooling, heating
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Last Changed indicates the last time the state of an entity was modified.
    /// </summary>
    public DateTimeOffset LastChanged { get; set; }

    /// <summary>
    /// Last Updated indicates the last time any change (state or attribute) was recorded for that entity.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}
