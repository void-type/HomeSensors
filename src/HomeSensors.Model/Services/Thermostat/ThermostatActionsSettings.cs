namespace HomeSensors.Model.Services.Thermostat;

public class ThermostatActionsSettings
{
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Home Assistant entity id of a template sensor that represents the state of the hvac_action of a thermostat.
    /// Template example: {{ state_attr('climate.ecobee_thermostat', 'hvac_action') }}
    /// We use a template sensor to reduce the amount of climate state changes polled by the history API.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Minutes between each check.
    /// </summary>
    public int BetweenTicksMinutes { get; init; } = 5;
}
