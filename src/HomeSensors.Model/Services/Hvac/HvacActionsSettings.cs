namespace HomeSensors.Model.Services.Hvac;

public class HvacActionsSettings
{
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Home Assistant entity id of a thermostat (climate) or a template sensor (sensor) that represents the state of the hvac_action of a thermostat.
    /// You can use a template sensor to reduce the amount of climate state changes polled by the history API.
    /// Template example: {{ state_attr('climate.ecobee_thermostat', 'hvac_action') }}
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Minutes between each check.
    /// </summary>
    public int BetweenTicksMinutes { get; init; } = 5;
}
