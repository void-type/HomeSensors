namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesResponse
{
    public TemperatureTimeSeriesResponse(List<TemperatureTimeSeriesThermostatAction> thermostatActions, List<TemperatureTimeSeriesLocationData> locationSeries)
    {
        ThermostatActions = thermostatActions;
        Locations = locationSeries;
    }

    public List<TemperatureTimeSeriesThermostatAction> ThermostatActions { get; }

    public List<TemperatureTimeSeriesLocationData> Locations { get; }
}
